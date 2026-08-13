using Garethp.ModsOfMistriaInstallerLib.Audio;
using Garethp.ModsOfMistriaInstallerLib.Lang;
using Garethp.ModsOfMistriaInstallerLib.Models.MOMI;
using Garethp.ModsOfMistriaInstallerLib.ModTypes;
using Garethp.ModsOfMistriaInstallerLib.Utils;
using Tomlyn;

namespace Garethp.ModsOfMistriaInstallerLib.Installer;

// Processes momi/audio/*.toml: replaces named tracks' audio inside vanilla
// .bank files with a mod's own WAVs, using the pipeline in
// ModsOfMistriaInstallerLib/Audio (see docs/AUDIO_REPLACEMENT.md).
//
// Reads the bank via IFileModifier, not the pristine backup, so an earlier
// mod's replacement to the same bank this run is preserved and composed
// with - the same rule every other installer follows for its own file type.
//
// FSBank rebuilds a whole FSB5 group at once, not just the changed subsound
// (see FsBankNative's class comment), so entries are grouped by bank and,
// within a bank, by which group their track lives in - each group is
// decoded/re-encoded once regardless of how many of its tracks change.
public class AudioInstaller(
    Dictionary<string, string> fileNameUidMapping,
    IFileModifier fileModifier)
    : Installer(fileNameUidMapping)
{
    public override void Install(
        IMod mod,
        GeneratedInformation generatedInformation,
        Action<string, string> reportStatus
    ) {
        if (!mod.HasFilesInFolder("momi/audio", ".toml"))
            return;

        var replacements = new List<AudioReplacementFile>();
        foreach (var file in mod.GetFilesInFolder("momi/audio", ".toml"))
        {
            var content = mod.ReadFile(file);
            if (string.IsNullOrWhiteSpace(content)) continue;

            Dictionary<string, AudioReplacementFile>? entries;
            try
            {
                entries = TomlSerializer.Deserialize<Dictionary<string, AudioReplacementFile>>(content);
            }
            catch (Exception e)
            {
                mod.GetValidation().AddError(mod, file, string.Format(Resources.CoreCouldNotParseFile, e.Message));
                continue;
            }

            if (entries is null) continue;

            foreach (var (track, entry) in entries)
            {
                entry.Id = track;
                var errorsBefore = mod.GetValidation().Errors.Count;
                entry.Validate(mod.GetValidation(), mod, file);
                if (mod.GetValidation().Errors.Count == errorsBefore) replacements.Add(entry);
            }
        }

        foreach (var group in replacements.GroupBy(r => r.Bank!))
        {
            var entries = group.ToList();
            try
            {
                InstallBank(mod, group.Key, entries, reportStatus);
            }
            catch (Exception e)
            {
                // Covers failures no earlier check catches: a malformed
                // bank, or the native FMOD/FSBank libraries this all runs
                // on being missing or broken. Isolated per bank, and never
                // left to propagate - an unhandled exception here would
                // abort ModInstaller's whole per-mod loop (no per-mod
                // isolation exists above this), taking every other mod's
                // install down with it, not just this one's audio.
                foreach (var entry in entries)
                    mod.GetValidation()
                        .AddError(mod, "momi/audio", string.Format(Resources.CoreErrorAudioProcessingFailed, entry.Id, e.Message));
            }
        }
    }

    private void InstallBank(
        IMod mod, string bankName, List<AudioReplacementFile> entries, Action<string, string> reportStatus
    ) {
        var dest = DestinationPath($"audio/{bankName}.bank");
        if (!fileModifier.Exists(dest))
        {
            foreach (var entry in entries)
                mod.GetValidation()
                    .AddError(mod, "momi/audio", string.Format(Resources.CoreErrorAudioBankNotFound, entry.Id, bankName));
            return;
        }

        byte[] bank;
        using (var stream = fileModifier.GetReadStream(dest))
        using (var mem = new MemoryStream())
        {
            stream.CopyTo(mem);
            bank = mem.ToArray();
        }

        var groupCount = FmodBankFile.ReadGroups(bank).Count;
        var decodedGroups = new List<FmodCoreNative.DecodedSubsound>?[groupCount];
        var locations = new Dictionary<AudioReplacementFile, int>(); // entry -> group index

        foreach (var entry in entries)
        {
            for (var g = 0; g < groupCount; g++)
            {
                decodedGroups[g] ??= FmodCoreNative.DecodeGroup(FmodBankFile.ExtractGroup(bank, g));
                if (decodedGroups[g]!.FindIndex(s => s.Name == entry.Id) < 0) continue;
                locations[entry] = g;
                break;
            }

            if (!locations.ContainsKey(entry))
                mod.GetValidation()
                    .AddError(mod, "momi/audio", string.Format(Resources.CoreErrorAudioTrackNotFound, entry.Id, bankName));
        }

        foreach (var group in locations.GroupBy(kv => kv.Value))
        {
            var decoded = decodedGroups[group.Key]!;
            var timelinePatches = new List<(int SubsoundIndex, uint NewSamples48K, float NewSeconds)>();

            foreach (var entry in group.Select(kv => kv.Key))
            {
                var index = decoded.FindIndex(s => s.Name == entry.Id);
                try
                {
                    var original = decoded[index];
                    using var wavStream = mod.ReadFileAsStream(entry.Wav!);
                    using var wavMem = new MemoryStream();
                    wavStream.CopyTo(wavMem);
                    var replacement = FmodCoreNative.FromWav(entry.Id, wavMem.ToArray());
                    decoded[index] = replacement;
                    var oldSamples = SamplesAt48K(original);
                    var newSamples = SamplesAt48K(replacement);
                    if (oldSamples != newSamples)
                        timelinePatches.Add((index, newSamples, (float)(newSamples / 48000.0)));
                    reportStatus($"Replacing audio track: {entry.Id}", "");
                }
                catch (Exception e)
                {
                    mod.GetValidation()
                        .AddError(mod, "momi/audio", string.Format(Resources.CoreErrorAudioWavUnreadable, entry.Id, e.Message));
                }
            }

            var rebuiltFsb = FsBankNative.EncodeGroup(decoded);
            bank = FmodBankFile.ReplaceGroup(bank, group.Key, rebuiltFsb);

            // Corrects the compiled FMOD event's own timeline length, which
            // otherwise still reflects the original track and cuts a longer
            // replacement off early (playlists advance, single-instrument
            // loops restart) - see docs/investigations/custom-music.md's
            // "Measured directly, not just inferred" section for how this
            // was found and confirmed via FMOD's own Studio API. The offsets
            // to patch are resolved structurally, by GUID, from this exact
            // subsound's own position in the group - not by searching for
            // the old duration's value, which turned out to be unsafe (the
            // same duration can legitimately belong to more than one
            // unrelated track). A track with nothing to patch (referenced by
            // zero timeline constructs) just leaves the bank as ReplaceGroup
            // produced it.
            //
            // A Scatterer-based track (e.g. a season's music playlist) also
            // needs its own SpawnTime pushed out - a Scatterer schedules its
            // next spawn on that independent timer regardless of whether the
            // current voice has finished, so leaving it at the original
            // (short) window spawns a second, overlapping voice on top of a
            // longer replacement partway through. Confirmed by tracing real
            // playback, not just GetLength() - see
            // FmodEventGraph.FindScattererSpawnTimeOffsets.
            //
            // SpawnTime is set to the replacement's own duration *plus* a
            // buffer, not the same value as the outer window - setting both
            // to exactly the same instant reproduces vanilla's relationship
            // backwards. Vanilla always had the outer window (~124s) fire
            // *before* the scatterer's own spawn timer (~150-180s) could
            // ever reach it, so in practice the spawn timer never actually
            // fired mid-track. FMOD's own Q&A forum confirms why the exact-
            // instant case is unsafe: a Scatterer's polyphony-limit voice
            // steal is a hard kill, not a graceful fade ("an event that's
            // still fading out is an event that's still consuming voices" -
            // Firelight staff), so there's no release tail masking a race if
            // the timeline's own reset and the spawn timer's steal land in
            // the same tick. 5 seconds tested clean in real gameplay after
            // 0 (unsafe - audible overlap) and 30 (safe, but a needlessly
            // long gap); this isn't a documented FMOD minimum, since none
            // exists publicly - it's this project's own empirical margin.
            const float spawnTimeBufferSeconds = 5f;
            foreach (var (subsoundIndex, newSamples, newSeconds) in timelinePatches)
            {
                bank = FmodBankFile.PatchPlaybackLengthFields(bank, group.Key, subsoundIndex, newSamples);
                bank = FmodBankFile.PatchScattererSpawnTime(bank, group.Key, subsoundIndex, newSeconds + spawnTimeBufferSeconds);
            }
        }

        if (locations.Count > 0)
            fileModifier.Write(dest, bank);
    }

    // The compiled event data was confirmed (via FMOD's own Studio API, see
    // docs/investigations/custom-music.md) to reference each track's sample
    // count at a fixed 48kHz timeline reference rate, regardless of a given
    // subsound's own actual sample rate - so this always converts to that
    // rate rather than assuming every subsound is already 48kHz.
    private static uint SamplesAt48K(FmodCoreNative.DecodedSubsound subsound)
    {
        var bytesPerSample = subsound.Channels * (subsound.Bits / 8);
        var sampleCount = subsound.Pcm.Length / (double)bytesPerSample;
        var seconds = sampleCount / subsound.SampleRate;
        return (uint)Math.Round(seconds * 48000.0);
    }
}
