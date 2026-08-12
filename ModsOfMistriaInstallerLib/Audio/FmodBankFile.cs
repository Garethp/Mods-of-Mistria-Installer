namespace Garethp.ModsOfMistriaInstallerLib.Audio;

// Pure container-format logic for FMOD Studio .bank files: a RIFF/"FEV "
// file whose PROJ/BNKI chunk carries an "SNDH" table of (offset, size)
// pairs, one per embedded FSB5 sub-bank ("group" below - Fmod-Bank-Tools
// calls these fsb[0], fsb[1], ... and each can itself bundle several named
// subsounds, e.g. MinesUpper.bank's one group holds three barks).
//
// Ported (logic, not code) from the GPLv3 Fmod-Bank-Tools project
// (https://github.com/Wouldubeinta/Fmod-Bank-Tools) - bank_extract.cpp's
// header walk and rebuild_worker.cpp's bankRebuild(). MOMI is GPLv3 too, so
// this is a compatible relicense, not a fresh reimplementation guess: the
// field names and the "unconditional re-seek after SNDH" quirk below are
// carried over deliberately, because the original never revisits the bytes
// it re-seeks past (the read loop's exit condition already tripped), and a
// "cleaned up" rewrite risks silently changing behavior on files this
// wasn't tested against.
//
// The container itself has no fixed-size slots: rebuilding recomputes every
// group's offset, so a replacement can be larger or smaller than the
// original freely. That is confirmed by hand against the real game, not
// just inferred from this parser (see docs/investigations/custom-music.md).
public static class FmodBankFile
{
    private const uint TagSndh = 0x48444E53; // "SNDH"
    private const uint TagStbl = 0x4C425453; // "STBL"
    private const uint TagSnd  = 0x20444E53; // "SND "

    public sealed record Group(uint Offset, uint Size);

    // The subset of the header walk needed to rebuild, not just list,
    // groups: where the SNDH table itself lives (to overwrite it), where
    // each group's "SND " chunk payload starts (to splice new bytes in),
    // and each group's original chunk padding (preserved verbatim, exactly
    // as the reference implementation does - its purpose isn't otherwise
    // documented, so the only safe move is to carry it forward unchanged).
    private sealed class Layout
    {
        public required List<Group> Groups;
        public required long SndhTableLocation;
        public required long[] SndLocation;
        public required uint[] SndBuffer;
    }

    // The (offset, size) table for every embedded FSB5 group. Enough to
    // enumerate groups and extract one; ReplaceGroup needs the fuller
    // Parse() below instead.
    public static List<Group> ReadGroups(byte[] bank)
    {
        using var stream = new MemoryStream(bank, writable: false);
        using var reader = new BinaryReader(stream);
        ReadPreamble(reader);

        while (stream.Position < stream.Length)
        {
            var chunkType = reader.ReadUInt32();
            var chunkSize = reader.ReadUInt32();
            if (chunkType == 0xFFFFFFFF || chunkSize == 0xFFFFFFFF)
                throw new InvalidDataException("fmod bank: invalid chunk while scanning for SNDH");

            if (chunkType == TagSndh)
                return ReadSndhTable(reader, chunkSize);

            stream.Position += chunkSize;
        }

        throw new InvalidDataException("fmod bank: no SNDH chunk found");
    }

    // Raw FSB5 bytes for one group, copied verbatim - no decoding.
    public static byte[] ExtractGroup(byte[] bank, int groupIndex)
    {
        var groups = ReadGroups(bank);
        if (groupIndex < 0 || groupIndex >= groups.Count)
            throw new ArgumentOutOfRangeException(nameof(groupIndex));

        var group = groups[groupIndex];
        var result = new byte[group.Size];
        Array.Copy(bank, group.Offset, result, 0, group.Size);
        return result;
    }

    // Rebuilds the whole bank with one group's FSB5 bytes replaced by
    // newGroupBytes, recomputing every later group's offset. Every other
    // group's bytes and padding are carried over from the original
    // unchanged. Mirrors rebuild_worker.cpp's bankRebuild().
    public static byte[] ReplaceGroup(byte[] bank, int groupIndex, byte[] newGroupBytes)
    {
        var layout = Parse(bank);
        if (groupIndex < 0 || groupIndex >= layout.Groups.Count)
            throw new ArgumentOutOfRangeException(nameof(groupIndex));

        var groupCount = layout.Groups.Count;
        var newSizes = new uint[groupCount];
        for (var i = 0; i < groupCount; i++)
            newSizes[i] = i == groupIndex ? (uint)newGroupBytes.Length : layout.Groups[i].Size;

        // Recompute offsets: group 0 keeps its original offset (the header
        // prefix before it never changes size), every later group starts
        // right after the previous one's new bytes plus that group's own
        // preserved padding, plus 8 for the "SND "+size tag pair - same
        // recurrence as the reference implementation.
        var newOffsets = new uint[groupCount];
        newOffsets[0] = layout.Groups[0].Offset;
        for (var i = 0; i < groupCount - 1; i++)
            newOffsets[i + 1] = newOffsets[i] + newSizes[i] + layout.SndBuffer[i + 1] + 8;

        using var output = new MemoryStream();
        using var writer = new BinaryWriter(output);

        // Header up to the first group is untouched.
        writer.Write(bank, 0, (int)layout.Groups[0].Offset);

        // Rewrite the SNDH offset/size table in place.
        output.Position = layout.SndhTableLocation;
        for (var i = 0; i < groupCount; i++)
        {
            writer.Write(newOffsets[i]);
            writer.Write(newSizes[i]);
        }

        // Rewrite each group's "SND " chunk: tag, declared size (padding +
        // fsb bytes, same convention as the original), the preserved
        // padding bytes, then the group's fsb bytes (original or new).
        output.Position = layout.SndLocation[0];
        for (var i = 0; i < groupCount; i++)
        {
            var groupBytes = i == groupIndex ? newGroupBytes : ExtractOriginalGroup(bank, layout.Groups[i]);

            writer.Write("SND "u8.ToArray());
            writer.Write(newSizes[i] + layout.SndBuffer[i]);
            if (layout.SndBuffer[i] > 0)
                writer.Write(new byte[layout.SndBuffer[i]]);
            writer.Write(groupBytes);
        }

        var result = output.ToArray();

        // RIFF's own size field: total length minus the 8-byte "RIFF"+size
        // prefix.
        var riffSize = (uint)(result.Length - 8);
        BitConverter.GetBytes(riffSize).CopyTo(result, 4);

        return result;
    }

    private static byte[] ExtractOriginalGroup(byte[] bank, Group group)
    {
        var result = new byte[group.Size];
        Array.Copy(bank, group.Offset, result, 0, group.Size);
        return result;
    }

    // RIFF/FEV/LIST/PROJ/BNKI preamble every .bank starts with, common to
    // both the simple and full parses. Leaves the stream positioned right
    // after BNKI's own chunk_size field is skipped, ready to walk chunks.
    private static void ReadPreamble(BinaryReader reader)
    {
        var stream = reader.BaseStream;

        stream.Position = 0;
        if (ReadTag(reader) != "RIFF") throw new InvalidDataException("fmod bank: missing RIFF magic");

        stream.Position = 0x08;
        if (ReadTag(reader) != "FEV ") throw new InvalidDataException("fmod bank: missing FEV tag");

        stream.Position = 0x14;
        if (reader.ReadUInt32() == 0) throw new InvalidDataException("fmod bank: invalid version");

        stream.Position = 0x1C;
        if (ReadTag(reader) != "LIST") throw new InvalidDataException("fmod bank: missing LIST tag");

        stream.Position += 0x04;
        var proj = ReadTag(reader);
        var bnki = ReadTag(reader);
        if (proj != "PROJ" || bnki != "BNKI")
            throw new InvalidDataException("fmod bank: missing PROJ/BNKI tags");

        var chunkSize = reader.ReadUInt32();
        stream.Position += chunkSize;
    }

    private static List<Group> ReadSndhTable(BinaryReader reader, uint chunkSize)
    {
        if (chunkSize == 0) throw new InvalidDataException("fmod bank: SNDH chunk has no groups");

        var fsbCount = (chunkSize - 4) / 8;
        reader.ReadUInt32(); // sndh_unknown - present in every real bank, meaning not otherwise established

        var groups = new List<Group>((int)fsbCount);
        for (var i = 0; i < fsbCount; i++)
        {
            var offset = reader.ReadUInt32();
            var size = reader.ReadUInt32();
            groups.Add(new Group(offset, size));
        }

        if (groups.Count == 0 || groups[0].Offset == 0 || groups[0].Size == 0)
            throw new InvalidDataException("fmod bank: SNDH table has a zero offset/size");

        return groups;
    }

    // Full header walk needed for rebuilding: SNDH (the offset/size table
    // itself and where it lives), STBL (skipped, with the reference
    // implementation's own alignment-probe quirk carried over verbatim),
    // and "SND " (where each group's chunk payload starts, and how much
    // padding it carries).
    private static Layout Parse(byte[] bank)
    {
        using var stream = new MemoryStream(bank, writable: false);
        using var reader = new BinaryReader(stream);
        ReadPreamble(reader);

        List<Group>? groups = null;
        long sndhLocation = 0;
        long[]? sndLocation = null;
        var sndBuffer = Array.Empty<uint>();

        while (stream.Position < stream.Length)
        {
            var chunkType = reader.ReadUInt32();
            var chunkSize = reader.ReadUInt32();
            if (chunkType == 0xFFFFFFFF || chunkSize == 0xFFFFFFFF)
                throw new InvalidDataException("fmod bank: invalid chunk while parsing");

            if (chunkType == TagSndh)
            {
                // +4 to skip past sndh_unknown: the table location must
                // point at the first (offset, size) pair, not the chunk
                // body's start, since that's where the rebuild write later
                // lands its rewritten pairs.
                sndhLocation = stream.Position + 4;
                groups = ReadSndhTable(reader, chunkSize);
                sndBuffer = new uint[groups.Count];
                sndLocation = new long[groups.Count];
                // No extra seek here: ReadSndhTable's reads already consumed
                // exactly chunkSize bytes (4 for sndh_unknown + 8 per group),
                // so the stream is already positioned at this chunk's end,
                // correctly lined up to read the next chunk's header. This
                // differs from the simple ReadGroups walk above, which
                // re-seeks unconditionally after every chunk including SNDH -
                // harmless there only because that loop returns immediately
                // once the SNDH table is found, so the extra seek is never
                // acted on. Here the walk must continue past SNDH to find
                // STBL/SND, so that extra seek would (and did, before this
                // fix) skip past them.
                continue;
            }

            if (chunkType == TagStbl)
            {
                var current = stream.Position;
                if (chunkSize != 0)
                {
                    stream.Position = current + chunkSize;
                    var probe = reader.ReadUInt32();
                    // The reference implementation peeks one uint32 past the
                    // declared chunk end; if it isn't "SND " or "HASH", the
                    // real boundary is one byte further than declared. Real
                    // observed alignment quirk in some STBL chunks, not a
                    // guess - carried over as-is.
                    if (probe != TagSnd && probe != 0x48534148 /* "HASH" */)
                        chunkSize += 1;
                }

                stream.Position = current + chunkSize;
                continue;
            }

            if (chunkType == TagSnd && groups is not null && sndLocation is not null)
            {
                sndLocation[0] = stream.Position - 8;
                sndBuffer[0] = chunkSize - groups[0].Size;

                for (var j = 0; j < groups.Count - 1; j++)
                {
                    sndLocation[j + 1] = groups[j].Offset + groups[j].Size;
                    stream.Position = sndLocation[j + 1] + 4;
                    var nextChunkSize = reader.ReadUInt32();
                    sndBuffer[j + 1] = nextChunkSize - groups[j + 1].Size;
                }

                break;
            }

            stream.Position += chunkSize;
        }

        if (groups is null) throw new InvalidDataException("fmod bank: no SNDH chunk found");
        if (sndLocation is null || sndLocation[0] == 0)
            throw new InvalidDataException("fmod bank: no SND chunk found");

        return new Layout
        {
            Groups = groups,
            SndhTableLocation = sndhLocation,
            SndLocation = sndLocation,
            SndBuffer = sndBuffer,
        };
    }

    private static string ReadTag(BinaryReader reader) =>
        System.Text.Encoding.ASCII.GetString(reader.ReadBytes(4));
}
