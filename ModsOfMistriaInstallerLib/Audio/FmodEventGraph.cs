namespace Garethp.ModsOfMistriaInstallerLib.Audio;

// Parses just enough of the FMOD Studio compiled "event graph" region of a
// .bank file (everything before the SNDH chunk - see FmodBankFile's own
// header comment for the container format around it) to find every field
// that governs how long a given subsound plays before its timeline advances
// or loops. See docs/investigations/custom-music.md for how this was found:
// a byte-value search for "the" length field turned out to be unsafe (the
// same duration can legitimately appear for more than one unrelated track),
// so this resolves the *specific* fields that reference a given subsound by
// walking the real object graph instead of guessing by value:
//
//   timeline -> trigger box -> instrument
//     - waveform instrument -> waveform resource -> subsound index (direct)
//     - multi instrument or scatterer instrument -> playlist -> member
//       instrument (a playlist member, sharing its container's one window -
//       this is why the same duration can legitimately show up for more
//       than one track: Fields of Mistria's Fall music turned out to use
//       *two* scatterer instruments with overlapping membership, each with
//       its own window, not one track's own individual length)
//     - event instrument -> nested event -> nested event's own timeline
//       (a sub-event reference, in case a track is wrapped one or more
//       levels away from the timeline actually driving playback)
//
// A timeline's TriggerBox.Length is read by EventDescription::GetLength(),
// but real playback (confirmed by tracing an actual FMOD Studio event
// instance, not just querying metadata) is governed by a sibling
// TransitionRegion's own Start/End - both need patching together for a
// replacement to actually play past the original's duration in-game.
//
// Chunk tags, field layouts and the walk's structure (list nesting via a
// parent stack, the null-terminator chunk-tag realignment quirk, the X16
// variable-length count encoding) are ported (logic, not code) from the
// Apache-2.0 Masusder/FModBankParser project
// (https://github.com/Masusder/FModBankParser), which documents this region
// of the format in far more detail than Fmod-Bank-Tools' own SNDH/STBL/SND
// walk (already ported into FmodBankFile). Apache-2.0 is compatible with
// MOMI's GPLv3, mirroring how Fmod-Bank-Tools' GPLv3 logic was carried over.
//
// Deliberately narrow: only the node types on the path from "timeline" to
// "subsound index" are parsed. Everything else in a chunk's body (sustain
// points, markers, envelope/volume/pitch fields, transition regions, buses,
// effects, snapshots...) is skipped over using the chunk's own declared
// size, the same way FmodBankFile treats chunks it doesn't need - so this
// never has to fully understand a node to stay correctly positioned for the
// next one.
public static class FmodEventGraph
{
    private const uint TagList = 0x5453494C; // "LIST"
    private const uint TagFormatInfo = 0x20544D46; // "FMT "
    private const uint TagEventBody = 0x42545645; // "EVTB"
    private const uint TagTimelineBody = 0x424E4C54; // "TLNB"
    private const uint TagMultiInstrumentBody = 0x4249554D; // "MUIB"
    private const uint TagScattererInstrumentBody = 0x42495053; // "SPIB"
    private const uint TagPlaylist = 0x54534C50; // "PLST"
    private const uint TagEventInstrumentBody = 0x42495645; // "EVIB"
    private const uint TagTransitionRegionBody = 0x424E5254; // "TRNB"
    private const uint TagWaveformInstrumentBody = 0x42494157; // "WAIB"
    private const uint TagWaveformResource = 0x20564157; // "WAV "

    public readonly struct Guid128 : IEquatable<Guid128>
    {
        private readonly uint _a, _b, _c, _d;

        public Guid128(BinaryReader reader)
        {
            _a = reader.ReadUInt32();
            _b = reader.ReadUInt32();
            _c = reader.ReadUInt32();
            _d = reader.ReadUInt32();
        }

        public bool Equals(Guid128 other) => _a == other._a && _b == other._b && _c == other._c && _d == other._d;
        public override bool Equals(object? obj) => obj is Guid128 g && Equals(g);
        public override int GetHashCode() => HashCode.Combine(_a, _b, _c, _d);
    }

    private readonly record struct TriggerBoxRef(Guid128 TargetGuid, long LengthFieldOffset);

    private sealed class Graph
    {
        public int FileVersion;
        public readonly Dictionary<Guid128, List<TriggerBoxRef>> TimelineTriggerBoxes = [];
        public readonly Dictionary<Guid128, List<long>> TimelineExtraOffsets = []; // sustain points + named marker lengths
        public readonly Dictionary<Guid128, Guid128> EventTimeline = []; // EventNode.BaseGuid -> TimelineGuid
        public readonly Dictionary<Guid128, Guid128> EventInstrumentToEvent = []; // EventInstrumentNode.BaseGuid -> EventGuid
        public readonly Dictionary<Guid128, List<Guid128>> PlaylistEntries = []; // MultiInstrument.BaseGuid -> member instrument guids
        public readonly Dictionary<Guid128, Guid128> WaveformInstrumentToResource = [];
        public readonly Dictionary<Guid128, (int SoundBankIndex, int SubsoundIndex)> WaveformResources = [];
        public readonly Dictionary<Guid128, (long MinOffset, long MaxOffset)> ScattererSpawnTimeOffsets = [];
    }

    // Byte offsets (into bank) of every field that governs playback length
    // of the subsound at (soundBankIndex, subsoundIndex): each referencing
    // TriggerBox's own Length, plus - on the same timeline - every
    // SustainPoint.Position, TimelineNamedMarker.Length, and (the field that
    // actually drives real-time looping/advancing, not just
    // EventDescription::GetLength() metadata - see ParseTransitionRegionBody)
    // sibling TransitionRegion.Start/End. All of them need to move together:
    // confirmed by tracing an actual FMOD Studio event instance through real
    // playback, not just querying metadata, that patching the trigger box
    // alone left playback looping at the original point. A subsound is
    // referenced directly, indirectly as one member of a Multi
    // Instrument/Scatterer playlist that's triggered as a whole (a shared
    // window that can legitimately cover several sibling tracks), or
    // indirectly through one or more levels of sub-event reference. Empty
    // means no timeline construct references this subsound at all (nothing
    // to patch, not an error - the audio swap itself still works, just
    // without the early-cutoff/loop fix).
    public static List<long> FindPlaybackLengthFieldOffsets(byte[] bank, int soundBankIndex, int subsoundIndex)
    {
        var graph = Parse(bank);
        var targets = ResolveTargets(graph, soundBankIndex, subsoundIndex);
        if (targets.Count == 0) return [];

        var offsets = new List<long>();
        foreach (var (timelineGuid, boxes) in graph.TimelineTriggerBoxes)
        {
            var matched = boxes.Where(tb => targets.Contains(tb.TargetGuid)).ToList();
            if (matched.Count == 0) continue;

            offsets.AddRange(matched.Select(tb => tb.LengthFieldOffset));
            if (graph.TimelineExtraOffsets.TryGetValue(timelineGuid, out var extraOffsets))
                offsets.AddRange(extraOffsets);
        }

        return offsets;
    }

    // Byte offsets of a Scatterer's own SpawnTime.Minimum/Maximum (float
    // seconds, not the samples-at-48kHz format everything above uses) for
    // every Scatterer the subsound at (soundBankIndex, subsoundIndex) is a
    // playlist member of. A Scatterer schedules its next spawn on this
    // independent timer regardless of whether the currently-playing voice
    // has actually finished - confirmed by tracing real playback with a
    // replacement long enough to still be mid-play when the original
    // ~150-180s window elapsed: a second voice started anyway, overlapping
    // the first. Left at its original (short) range, a replacement much
    // longer than that range gets a second, unwanted voice spawned on top of
    // it partway through - a different failure mode from the
    // trigger-box/transition-region cutoff FindPlaybackLengthFieldOffsets
    // addresses, and one that only showed up through real playback tracing,
    // never through GetLength() or offline analysis. Empty means the
    // subsound isn't a Scatterer member at all (e.g. it's a plain
    // single-instrument track, or a Multi Instrument's own member, which
    // doesn't have this construct).
    public static List<long> FindScattererSpawnTimeOffsets(byte[] bank, int soundBankIndex, int subsoundIndex)
    {
        var graph = Parse(bank);
        var targets = ResolveTargets(graph, soundBankIndex, subsoundIndex);
        if (targets.Count == 0) return [];

        var offsets = new List<long>();
        foreach (var scattererGuid in targets)
        {
            if (!graph.ScattererSpawnTimeOffsets.TryGetValue(scattererGuid, out var spawnTime)) continue;
            offsets.Add(spawnTime.MinOffset);
            offsets.Add(spawnTime.MaxOffset);
        }

        return offsets;
    }

    // Every construct that transitively references the subsound at
    // (soundBankIndex, subsoundIndex): its own waveform instrument(s), any
    // Multi Instrument/Scatterer it's a playlist member of, and any
    // sub-event reference chain leading to those. Shared by both offset
    // resolvers above so they always agree on what counts as "referencing
    // this subsound."
    private static HashSet<Guid128> ResolveTargets(Graph graph, int soundBankIndex, int subsoundIndex)
    {
        var targetWaveformInstruments = new HashSet<Guid128>(
            graph.WaveformInstrumentToResource
                .Where(kv => graph.WaveformResources.TryGetValue(kv.Value, out var loc)
                             && loc.SoundBankIndex == soundBankIndex && loc.SubsoundIndex == subsoundIndex)
                .Select(kv => kv.Key));

        if (targetWaveformInstruments.Count == 0) return [];

        var targets = new HashSet<Guid128>(targetWaveformInstruments);

        // Fixpoint expansion: a multi-instrument is a target once any of its
        // playlist members is a target, and an event-instrument (a sub-event
        // reference) is a target once anything on the referenced event's own
        // timeline is a target. Either can newly qualify the other on a
        // later pass (a sub-event's timeline can itself trigger a multi
        // instrument, and vice versa), so repeat until nothing changes.
        bool changed;
        do
        {
            changed = false;

            foreach (var (multiInstrumentGuid, members) in graph.PlaylistEntries)
            {
                if (!targets.Contains(multiInstrumentGuid) && members.Any(targets.Contains))
                {
                    targets.Add(multiInstrumentGuid);
                    changed = true;
                }
            }

            foreach (var (eventInstrumentGuid, eventGuid) in graph.EventInstrumentToEvent)
            {
                if (targets.Contains(eventInstrumentGuid)) continue;
                if (!graph.EventTimeline.TryGetValue(eventGuid, out var timelineGuid)) continue;
                if (!graph.TimelineTriggerBoxes.TryGetValue(timelineGuid, out var boxes)) continue;
                if (boxes.Any(b => targets.Contains(b.TargetGuid)))
                {
                    targets.Add(eventInstrumentGuid);
                    changed = true;
                }
            }
        } while (changed);

        return targets;
    }

    private static Graph Parse(byte[] bank)
    {
        var sndhPos = FmodBankFile.FindSndhTagPosition(bank);
        var graph = new Graph();

        using var stream = new MemoryStream(bank, writable: false);
        using var reader = new BinaryReader(stream);

        // The FMT chunk (tag+size+FileVersion+CompatVersion, 16 bytes) sits
        // between the FEV header and the PROJ list, at a fixed offset -
        // ReadPreamble already relies on this (it validates the FileVersion
        // field is non-zero at 0x14) but only as a sanity check, without
        // exposing the value. Several node layouts below are gated on this
        // version, so it has to be read here before walking the rest.
        stream.Position = 0x14;
        graph.FileVersion = reader.ReadInt32();

        FmodBankFile.ReadPreamble(reader);
        Guid128? currentTimelineGuid = null;
        ParseNodes(reader, stream.Position, sndhPos, graph, ref currentTimelineGuid);
        return graph;
    }

    // Mirrors FModReader.ParseNodes: a flat walk where "LIST" chunks recurse
    // with a *fresh* parent stack scoped to that list's own direct children
    // (matching the reference exactly - a Multi Instrument's MUIB and its
    // PLST sibling are always direct children of the same enclosing "MUIT"
    // list, so a stack scoped to just that recursion level is sufficient to
    // associate them without threading state across nesting levels).
    //
    // currentTimelineGuid is the one exception: it's threaded by ref through
    // every recursive call (not re-scoped per level), because a
    // TransitionRegion is a sibling of the TimelineNode it governs only one
    // level further up than that reasoning suggests - TLNB sits inside its
    // own wrapping "LIST(TMLN)" and TRNB inside its own wrapping "LIST(TRAN)",
    // both themselves siblings under the enclosing event. Threading by ref
    // lets "the most recently seen timeline" survive returning out of the
    // TMLN list's own recursive call so the later TRAN list's call can still
    // see it - correct as long as chunks appear in file order per event,
    // confirmed against the real layout (TLNB, TRAN, TLNB, TRAN, ...).
    private static void ParseNodes(BinaryReader reader, long start, long end, Graph graph, ref Guid128? currentTimelineGuid)
    {
        reader.BaseStream.Position = start;
        var multiInstrumentStack = new Stack<Guid128>();

        while (reader.BaseStream.Position + 8 <= end)
        {
            var nodeStart = reader.BaseStream.Position;
            var rawTag = reader.ReadInt32();

            // Same realignment quirk as the reference parser: some chunks'
            // end is followed by a stray null byte before the next tag.
            if ((rawTag & 0xFF) == 0x00)
            {
                nodeStart = reader.BaseStream.Position - 3;
                reader.BaseStream.Position -= 3;
                rawTag = reader.ReadInt32();
            }

            var tag = unchecked((uint)rawTag);
            var size = reader.ReadUInt32();
            var nextNode = nodeStart + 8 + size;
            if (nextNode > end) break; // malformed/truncated relative to our own bound - stop rather than misread

            if (size == 0)
            {
                reader.BaseStream.Position = nextNode;
                continue;
            }

            switch (tag)
            {
                case TagList:
                    reader.ReadInt32(); // inner list-id tag - dispatch happens on child tags instead, same as reference
                    ParseNodes(reader, reader.BaseStream.Position, nextNode, graph, ref currentTimelineGuid);
                    break;

                case TagFormatInfo:
                    graph.FileVersion = reader.ReadInt32();
                    break;

                case TagEventBody:
                {
                    var guid = new Guid128(reader);
                    _ = new Guid128(reader); // SnapshotGuid
                    var timelineGuid = new Guid128(reader);
                    graph.EventTimeline[guid] = timelineGuid;
                    break;
                }

                case TagTimelineBody:
                    currentTimelineGuid = ParseTimelineBody(reader, graph);
                    break;

                case TagTransitionRegionBody:
                    ParseTransitionRegionBody(reader, graph, currentTimelineGuid);
                    break;

                // Scatterer instruments (randomized ambient one-shot
                // placement) carry a "PLST" playlist body the exact same way
                // Multi Instruments do - both get pushed here so the
                // following PLST chunk can attach to whichever is on top.
                case TagMultiInstrumentBody:
                {
                    var guid = new Guid128(reader);
                    multiInstrumentStack.Push(guid);
                    if (!graph.PlaylistEntries.ContainsKey(guid))
                        graph.PlaylistEntries[guid] = [];
                    break;
                }

                // A Scatterer schedules its next spawn on SpawnTime.Minimum/
                // Maximum regardless of whether the current voice is still
                // playing - see FindScattererSpawnTimeOffsets for why this
                // needs patching too, not just the outer trigger box/
                // transition region.
                case TagScattererInstrumentBody:
                {
                    var guid = new Guid128(reader);
                    reader.ReadInt32(); // MaximumSpawnPolyphony
                    reader.ReadInt32(); // SpawnCount
                    var minOffset = reader.BaseStream.Position;
                    reader.ReadSingle(); // SpawnTime.Minimum
                    var maxOffset = reader.BaseStream.Position;
                    reader.ReadSingle(); // SpawnTime.Maximum
                    graph.ScattererSpawnTimeOffsets[guid] = (minOffset, maxOffset);

                    multiInstrumentStack.Push(guid);
                    if (!graph.PlaylistEntries.ContainsKey(guid))
                        graph.PlaylistEntries[guid] = [];
                    break;
                }

                case TagPlaylist:
                {
                    if (multiInstrumentStack.TryPeek(out var owner))
                    {
                        reader.ReadInt32(); // PlayMode - not needed to locate the length field
                        reader.ReadInt32(); // SelectionMode
                        graph.PlaylistEntries[owner] = ReadElemList(reader, ReadPlaylistEntryGuid);
                        if (graph.FileVersion is >= 0x65 and <= 0x67)
                            reader.ReadBoolean();
                        multiInstrumentStack.Pop();
                    }
                    break;
                }

                case TagEventInstrumentBody:
                {
                    var guid = new Guid128(reader);
                    var eventGuid = new Guid128(reader);
                    graph.EventInstrumentToEvent[guid] = eventGuid;
                    break;
                }

                case TagWaveformInstrumentBody:
                {
                    var guid = new Guid128(reader);
                    if (graph.FileVersion < 0x46)
                        reader.ReadUInt32(); // legacy loading mode
                    var resourceGuid = new Guid128(reader);
                    graph.WaveformInstrumentToResource[guid] = resourceGuid;
                    break;
                }

                case TagWaveformResource:
                {
                    var guid = new Guid128(reader);
                    reader.ReadUInt16(); // payload size following the guid
                    var soundBankIndex = reader.ReadInt32();
                    var subsoundIndex = reader.ReadInt32();
                    graph.WaveformResources[guid] = (soundBankIndex, subsoundIndex);
                    break;
                }
            }

            if (reader.BaseStream.Position != nextNode)
                reader.BaseStream.Position = nextNode;
        }
    }

    // Reads TriggerBoxes/TimeLockedTriggerBoxes, (version >= 0x84) the
    // SustainPoints that follow them, and the TimelineNamedMarkers after
    // that - tempo markers after those are left unread; the caller's
    // chunk-size-based repositioning handles resyncing to the next chunk
    // regardless. Returns the timeline's own BaseGuid so the caller can
    // associate a sibling TransitionRegion with it (see
    // ParseTransitionRegionBody).
    //
    // A trigger box's own Length turned out not to be the whole story: it's
    // read by EventDescription::GetLength() and does get honored on cold
    // start, but real playback (traced via an actual event instance, not
    // just queried metadata) kept looping at the *original* point even after
    // patching Length, SustainPoint.Position and TimelineNamedMarker.Length
    // together. The field that actually governs the real-time loop/advance
    // turned out to be a sibling TransitionRegionNode's own Start/End (see
    // ParseTransitionRegionBody) - SustainPoints and named markers are still
    // read and patched here since they're legitimate, harmless-to-update
    // metadata, just not what drives playback.
    private static Guid128 ParseTimelineBody(BinaryReader reader, Graph graph)
    {
        var baseGuid = new Guid128(reader);
        if (graph.FileVersion < 0x6D)
            _ = new Guid128(reader); // legacy guid

        var triggerBoxes = ReadElemList(reader, ReadTriggerBox);
        var timeLockedTriggerBoxes = ReadElemList(reader, ReadTriggerBox);

        var all = new List<TriggerBoxRef>(triggerBoxes.Count + timeLockedTriggerBoxes.Count);
        all.AddRange(triggerBoxes);
        all.AddRange(timeLockedTriggerBoxes);
        graph.TimelineTriggerBoxes[baseGuid] = all;

        var extraOffsets = new List<long>();

        if (graph.FileVersion >= 0x84)
            extraOffsets.AddRange(ReadVersionedElemList(reader, ReadSustainPointPositionOffset));

        extraOffsets.AddRange(
            ReadVersionedElemList(reader, r => ReadTimelineNamedMarkerLengthOffset(r, graph))
                .Where(offset => offset.HasValue)
                .Select(offset => offset!.Value));

        graph.TimelineExtraOffsets[baseGuid] = extraOffsets;
        return baseGuid;
    }

    // A TransitionRegion's Start/End (both usually equal - a single jump
    // point, not a real range) mark where the timeline actually loops or
    // advances during real playback - confirmed via FMOD's own Studio API by
    // tracing a live event instance, not just querying GetLength(). Its
    // DestinationGuid points to where playback jumps to (a TimelineNamedMarker
    // in Fields of Mistria's own banks, e.g. back to position 0), which
    // isn't needed here since the Start/End fields are what must move to
    // extend playback - only their offsets are recorded, tied to whichever
    // timeline this transition region is a sibling of.
    private static void ParseTransitionRegionBody(BinaryReader reader, Graph graph, Guid128? owningTimelineGuid)
    {
        _ = new Guid128(reader); // BaseGuid
        _ = new Guid128(reader); // DestinationGuid
        var startOffset = reader.BaseStream.Position;
        reader.ReadUInt32(); // Start
        var endOffset = reader.BaseStream.Position;
        reader.ReadUInt32(); // End

        if (owningTimelineGuid is not { } timelineGuid) return;

        if (!graph.TimelineExtraOffsets.TryGetValue(timelineGuid, out var offsets))
            graph.TimelineExtraOffsets[timelineGuid] = offsets = [];
        offsets.Add(startOffset);
        offsets.Add(endOffset);
    }

    private static TriggerBoxRef ReadTriggerBox(BinaryReader reader)
    {
        var guid = new Guid128(reader);
        reader.ReadUInt32(); // StartTime
        var lengthFieldOffset = reader.BaseStream.Position;
        reader.ReadUInt32(); // Length - value itself unused here, only its offset matters
        return new TriggerBoxRef(guid, lengthFieldOffset);
    }

    private static long ReadSustainPointPositionOffset(BinaryReader reader)
    {
        var positionOffset = reader.BaseStream.Position;
        reader.ReadUInt32(); // Position - value itself unused here, only its offset matters

        // The evaluator list is self-delimiting (its own byte count comes
        // first), so it can be skipped without understanding evaluator
        // internals at all.
        var evaluatorsSize = reader.ReadInt32();
        if (evaluatorsSize > 0)
            reader.BaseStream.Position += evaluatorsSize;

        return positionOffset;
    }

    private static long? ReadTimelineNamedMarkerLengthOffset(BinaryReader reader, Graph graph)
    {
        _ = new Guid128(reader); // BaseGuid
        reader.ReadUInt32(); // Position - not the field that matters here, see above
        var nameLength = ReadX16(reader); // byte length, not an element count - no >>1
        if (nameLength > 0)
            reader.BaseStream.Position += nameLength;

        if (graph.FileVersion < 0x79) return null;

        var lengthFieldOffset = reader.BaseStream.Position;
        reader.ReadUInt32();
        return lengthFieldOffset;
    }

    private static Guid128 ReadPlaylistEntryGuid(BinaryReader reader)
    {
        var guid = new Guid128(reader);
        reader.ReadSingle(); // Weight - not needed to locate the length field
        return guid;
    }

    // FMOD's "X16" variable-length count prefix: a 16-bit value whose top
    // bit signals a second 16-bit word extending it to 31 bits.
    private static uint ReadX16(BinaryReader reader)
    {
        var low = unchecked((ushort)reader.ReadInt16());
        uint value = low;
        if ((low & 0x8000) != 0)
        {
            var high = reader.ReadUInt16();
            value &= 0x7FFFu;
            value |= (uint)high << 15;
        }
        return value;
    }

    private static List<T> ReadElemList<T>(BinaryReader reader, Func<BinaryReader, T> readElem)
    {
        var raw = ReadX16(reader);
        var count = (int)(raw >> 1);
        if (count <= 0) return [];

        reader.ReadUInt16(); // payload size
        var result = new List<T>(count);
        for (var i = 0; i < count; i++)
            result.Add(readElem(reader));
        return result;
    }

    // Same count encoding as ReadElemList, but each element carries its own
    // payload-size prefix instead of one shared for the whole list.
    private static List<T> ReadVersionedElemList<T>(BinaryReader reader, Func<BinaryReader, T> readElem)
    {
        var raw = ReadX16(reader);
        var count = (int)(raw >> 1);
        if (count <= 0) return [];

        var result = new List<T>(count);
        for (var i = 0; i < count; i++)
        {
            reader.ReadUInt16(); // per-element payload size
            result.Add(readElem(reader));
        }
        return result;
    }
}
