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
            InstallBank(mod, group.Key, group.ToList(), reportStatus);
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

            foreach (var entry in group.Select(kv => kv.Key))
            {
                var index = decoded.FindIndex(s => s.Name == entry.Id);
                try
                {
                    using var wavStream = mod.ReadFileAsStream(entry.Wav!);
                    using var wavMem = new MemoryStream();
                    wavStream.CopyTo(wavMem);
                    decoded[index] = FmodCoreNative.FromWav(entry.Id, wavMem.ToArray());
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
        }

        if (locations.Count > 0)
            fileModifier.Write(dest, bank);
    }
}
