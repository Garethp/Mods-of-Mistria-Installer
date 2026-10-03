using Garethp.ModsOfMistriaInstallerLib.Audio;
using Garethp.ModsOfMistriaInstallerLib.ModTypes;
using Garethp.ModsOfMistriaInstallerLib.Utils;
using Tomlyn;

namespace Garethp.ModsOfMistriaInstallerLib.Installer;

// Allows for ADDING and REDIRECTING(Replacing) audio. Modders need FMOD studio to build their banks for adding/replacing.
//
// ADD: make a custom event callable by a new name.
//     audio/<Name>.bank + audio/<Name>.meta.toml (asset_kind = "AudioBank"):
//          the content bank, copied into assets/audio so FMOD loads it. Routes the
//          event output to a Fields of Mistria bus (e.g. bus:/Music) when building.
//     audio/<Name>.strings.bank:
//          NOT installed. Only read to harvest the mod's "event:/…" -> GUID pairs,
//          which are merged (sorted) into the game's Master.strings.bank so Tango's
//          getPath resolves them and tango_play("<Name>") works.
//
// REDIRECT: makes an EXISTING (vanilla) event name play the mod's audio instead, without touching any GML call site.
//      audio/redirect.toml — "<vanilla event name>" = "<this mod's event name>":
//          repoints the vanilla name in Master.strings at the mod event's GUID, so
//          every by name play of it (literal or dynamically built) resolves to the
//          mod's event. The vanilla event's own GUID drops out of the table, so the
//          original never plays and there is no load-order collision (but that means the vanilla sound previously associated with it
//          cannot be played).
//
public class AudioInstaller(
    Dictionary<string, string> fileNameUidMapping,
    IFileModifier fileModifier)
    : Installer(fileNameUidMapping)
{
    private const string MasterStringsRel = "audio/Master.strings.bank";
    private const string DefaultBus = "Music";

    private readonly record struct Redirect(string VanillaPath, byte[] Guid, string Label);

    public override void Install(IMod mod, GeneratedInformation generatedInformation, Action<string, string> reportStatus)
    {
        var hasBanks = mod.HasFilesInFolder("audio", ".bank");
        var hasRedirects = mod.FileExists("audio/redirect.toml");
        if (!hasBanks && !hasRedirects) return;

        var contentBanks = new List<string>();
        var stringsBanks = new List<string>();
        if (hasBanks)
        {
            foreach (var rel in mod.GetFilesInFolder("audio", ".bank"))
            {
                if (rel.Replace('\\', '/').EndsWith(".strings.bank", StringComparison.OrdinalIgnoreCase))
                    stringsBanks.Add(rel);
                else
                    contentBanks.Add(rel);
            }
        }

        // The mod's own event name -> GUID map and its project master bus GUID, from the strings bank(s) it ships.
        var (modEvents, modMasterBus) = HarvestMod(mod, stringsBanks);

        // Parse redirects up front so ADD can skip any event that a redirect targets since
        // adding it under its own name too would put its GUID in the table twice and make getPath ambiguous.
        var redirects = ParseRedirects(mod, modEvents, reportStatus);
        var redirectTargets = new HashSet<string>(
            redirects.Select(r => Convert.ToHexString(r.Guid)), StringComparer.Ordinal);

        // 1. Merge new event names (ADD).
        MergeEventNames(mod, modEvents, redirectTargets, contentBanks.Count, reportStatus);

        // 2. Install each content bank (+ its .meta.toml) into assets/audio, rewriting its output bus to a game one.
        var busConfig = ParseBusConfig(mod, reportStatus);
        var gameBuses = ReadGameBuses();
        foreach (var bankRel in contentBanks)
            InstallContentBank(mod, bankRel, modMasterBus, busConfig, gameBuses, reportStatus);

        // 3. Repoint vanilla names at the mod's events (REDIRECT).
        ApplyRedirects(redirects, reportStatus);
    }

    private (Dictionary<string, byte[]> Events, byte[]? MasterBus) HarvestMod(IMod mod, List<string> stringsBanks)
    {
        var events = new Dictionary<string, byte[]>(StringComparer.Ordinal);
        byte[]? masterBus = null;
        foreach (var stringsRel in stringsBanks)
        {
            var modStrings = ReadAllBytes(mod.ReadFileAsStream(stringsRel));
            foreach (var (path, guid) in StringsBank.ReadPaths(modStrings))
            {
                if (path.StartsWith("event:/", StringComparison.Ordinal))
                    events[path] = guid; // bus:/ bank:/ vca:/ are the mod's private mixer so we ignore them...
                else if (path == "bus:/")
                    masterBus ??= guid; // ...except the project master bus, which we rewrite to a game bus on install.
            }
        }
        return (events, masterBus);
    }

    // "bus:/Music" -> GUID, read live from the game's own Master.strings.bank so nothing is hardcoded.
    private Dictionary<string, byte[]> ReadGameBuses()
    {
        var buses = new Dictionary<string, byte[]>(StringComparer.Ordinal);
        var masterPath = DestinationPath(MasterStringsRel);
        if (!fileModifier.Exists(masterPath)) return buses;
        var master = ReadAllBytes(fileModifier.GetReadStream(masterPath));
        foreach (var (path, guid) in StringsBank.ReadPaths(master))
            if (path.StartsWith("bus:/", StringComparison.Ordinal)) buses[path] = guid;
        return buses;
    }

    // Optional audio/buses.toml maps a content bank name (without .bank) to a game bus name
    // ("Music", "SoundEffects", "AmbienceAndEnvironment", "Reverb", "Master"). Default Music.
    private Dictionary<string, string> ParseBusConfig(IMod mod, Action<string, string> reportStatus)
    {
        var config = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (!mod.FileExists("audio/buses.toml")) return config;
        try
        {
            var map = TomlSerializer.Deserialize<Dictionary<string, string>>(mod.ReadFile("audio/buses.toml"));
            if (map is not null) foreach (var (k, v) in map) config[k] = v;
        }
        catch (Exception e)
        {
            reportStatus($"Audio: couldn't parse audio/buses.toml — {e.Message}", "");
        }
        return config;
    }

    private List<Redirect> ParseRedirects(IMod mod, Dictionary<string, byte[]> modEvents, Action<string, string> reportStatus)
    {
        var result = new List<Redirect>();
        if (!mod.FileExists("audio/redirect.toml")) return result;

        Dictionary<string, string>? map;
        try
        {
            map = TomlSerializer.Deserialize<Dictionary<string, string>>(mod.ReadFile("audio/redirect.toml"));
        }
        catch (Exception e)
        {
            reportStatus($"Audio: couldn't parse audio/redirect.toml — {e.Message}", "");
            return result;
        }
        if (map is null) return result;

        foreach (var (vanillaName, targetName) in map)
        {
            var vanillaPath = ToEventPath(vanillaName);
            var targetPath = ToEventPath(targetName);
            if (!modEvents.TryGetValue(targetPath, out var guid))
            {
                reportStatus($"Audio: redirect target '{targetName}' not found in this mod's .strings.bank — skipping '{vanillaName}'.", "");
                continue;
            }
            result.Add(new Redirect(vanillaPath, guid, $"{vanillaName} → {targetName}"));
        }
        return result;
    }

    private void MergeEventNames(
        IMod mod, Dictionary<string, byte[]> modEvents, HashSet<string> redirectTargets,
        int contentBankCount, Action<string, string> reportStatus)
    {
        if (modEvents.Count == 0)
        {
            if (contentBankCount > 0)
                reportStatus($"Audio: {mod.GetName()} ships content banks but no .strings.bank — its new events won't be callable by name.", "");
            return;
        }

        var masterPath = DestinationPath(MasterStringsRel);
        if (!fileModifier.Exists(masterPath))
        {
            reportStatus("Audio: game Master.strings.bank not found; skipping event-name merge.", "");
            return;
        }

        var master = ReadAllBytes(fileModifier.GetReadStream(masterPath));
        var existing = new HashSet<string>(
            StringsBank.ReadPaths(master).Select(p => p.Path), StringComparer.Ordinal);

        var added = 0;
        foreach (var (path, guid) in modEvents)
        {
            // Reached instead via a vanilla name — don't also add it under its own.
            if (redirectTargets.Contains(Convert.ToHexString(guid))) continue;

            if (!existing.Add(path))
            {
                reportStatus($"Audio: skipping '{path}' — an event with that name already exists.", "");
                continue;
            }

            master = StringsBank.Insert(master, path, guid);
            added++;
            reportStatus($"Audio: registered new event '{path}'", "");
        }

        if (added > 0)
            fileModifier.Write(masterPath, master);
    }

    private void ApplyRedirects(List<Redirect> redirects, Action<string, string> reportStatus)
    {
        if (redirects.Count == 0) return;

        var masterPath = DestinationPath(MasterStringsRel);
        if (!fileModifier.Exists(masterPath))
        {
            reportStatus("Audio: game Master.strings.bank not found; skipping redirects.", "");
            return;
        }

        var master = ReadAllBytes(fileModifier.GetReadStream(masterPath));
        var changed = 0;
        foreach (var redirect in redirects)
        {
            try
            {
                master = StringsBank.Repoint(master, redirect.VanillaPath, redirect.Guid);
                changed++;
                reportStatus($"Audio: redirected {redirect.Label}", "");
            }
            catch (Exception e)
            {
                reportStatus($"Audio: couldn't redirect {redirect.Label} — {e.Message}", "");
            }
        }

        if (changed > 0)
            fileModifier.Write(masterPath, master);
    }

    private void InstallContentBank(
        IMod mod, string bankRel, byte[]? modMasterBus,
        Dictionary<string, string> busConfig, Dictionary<string, byte[]> gameBuses,
        Action<string, string> reportStatus)
    {
        var normalised = bankRel.Replace('\\', '/');
        var fileName = Path.GetFileName(normalised);
        var bankName = Path.GetFileNameWithoutExtension(fileName);
        var bank = ReadAllBytes(mod.ReadFileAsStream(bankRel));

        // Rewrite the mod project's master bus to the requested game bus (default Music) so the event
        // lands on a real Fields of Mistria bus and obeys its volume slider. FMOD dedups buses by GUID.
        var busName = busConfig.GetValueOrDefault(bankName, DefaultBus);
        var busPath = busName.Equals("Master", StringComparison.OrdinalIgnoreCase) || busName.Length == 0
            ? "bus:/" : "bus:/" + busName;

        if (modMasterBus is null)
            reportStatus($"Audio: {fileName} has no project bus in its .strings.bank; leaving routing untouched.", "");
        else if (!gameBuses.TryGetValue(busPath, out var targetBus))
            reportStatus($"Audio: {fileName} target bus '{busPath}' not found in the game; leaving routing untouched.", "");
        else if (ReplaceBytesInPlace(bank, modMasterBus, targetBus) > 0)
            reportStatus($"Audio: routed {fileName} to {busPath}", "");

        fileModifier.Write(DestinationPath($"audio/{fileName}"), bank);

        // Copy the sidecar meta.toml so the asset database loads the bank.
        var metaRel = normalised[..^".bank".Length] + ".meta.toml";
        if (mod.FileExists(metaRel))
        {
            var metaName = Path.GetFileName(metaRel);
            fileModifier.Write(DestinationPath($"audio/{metaName}"), mod.ReadFile(metaRel));
            reportStatus($"Audio: installed bank {fileName}", "");
        }
        else
        {
            reportStatus($"Audio: {fileName} has no .meta.toml — it will not be loaded by the game.", "");
        }
    }

    // Replaces every 16 byte occurrence of `from` with `to` (same length) in place.
    private static int ReplaceBytesInPlace(byte[] data, byte[] from, byte[] to)
    {
        var count = 0;
        for (var i = 0; i + from.Length <= data.Length; i++)
        {
            var match = true;
            for (var j = 0; j < from.Length; j++)
                if (data[i + j] != from[j]) { match = false; break; }
            if (!match) continue;
            Array.Copy(to, 0, data, i, to.Length);
            i += from.Length - 1;
            count++;
        }
        return count;
    }

    private static string ToEventPath(string name) =>
        name.StartsWith("event:/", StringComparison.Ordinal) ? name : "event:/" + name;

    private static byte[] ReadAllBytes(Stream stream)
    {
        using (stream)
        using (var ms = new MemoryStream())
        {
            stream.CopyTo(ms);
            return ms.ToArray();
        }
    }
}
