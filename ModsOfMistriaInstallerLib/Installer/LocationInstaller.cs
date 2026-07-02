using System.Text.RegularExpressions;
using Garethp.ModsOfMistriaInstallerLib.Generator;
using Garethp.ModsOfMistriaInstallerLib.ModTypes;
using Garethp.ModsOfMistriaInstallerLib.Operations;
using Garethp.ModsOfMistriaInstallerLib.Utils;
using Tomlyn;
using Tomlyn.Model;

namespace Garethp.ModsOfMistriaInstallerLib.Installer;

// Pre-pass installer that runs over ALL mods before the per-mod loop.
//
// Problem it solves
// -----------------
// Fields of Mistria assigns each location a LocationId integer equal to its
// 0-based position in the alphabetically sorted list in locations.toml.  Tiled
// saves transition destinations as these positional integers.  If two mods each
// add a new location the combined sort shifts positions, so one mod's hardcoded
// destination_id value points to the wrong room.
//
// Fix
// ---
// 1. Collect every new location from every mod's momi/locations/*.toml.
// 2. Build the *final* global list (vanilla + all mods, sorted alphabetically).
// 3. For each mod, reconstruct its *local* list (vanilla + only that mod's
//    locations, sorted) — this is what the mod author saw when saving in Tiled.
// 4. Build local→name and name→globalId translation maps.
// 5. Patch destination_id values in every TMX file under each mod's tiled/.
// 6. Copy patched TMX files to assets/tiled/ and write the merged
//    locations.toml to assets/fiddle/locations.toml.
//
// Assumption: mod authors develop against vanilla + their own mod only.
//
public class LocationInstaller
{
    private readonly string _assetsLocation;
    private readonly InstallManifest _manifest;
    private readonly IFileModifier _fileModifier;

    private static readonly Regex DestinationIdRegex = new(
        @"(<property\s+name=""destination_id""\s+type=""int""\s+propertytype=""LocationId""\s+value="")(\d+)("")",
        RegexOptions.Compiled);

    public LocationInstaller(string fomLocation, InstallManifest manifest, IFileModifier fileModifier)
    {
        _assetsLocation = Path.Combine(fomLocation, "assets");
        _manifest       = manifest;
        _fileModifier = fileModifier;
    }

    public void Install(IEnumerable<IMod> mods, Action<string, string> reportStatus)
    {
        var modList = mods.ToList();

        // ── 1. Read vanilla location keys (vanilla is restored before this runs) ──
        var vanillaTable = TomlSerializer.Deserialize<TomlTable>(_fileModifier.Read("assets/fiddle/locations.toml"));
        var vanillaKeys  = SortedLocationKeys(vanillaTable);

        // ── 2. Collect new locations from every mod ────────────────────────────
        var modNewLocations = new Dictionary<IMod, List<LocationDefinition>>();
        var allNewLocations = new Dictionary<string, LocationDefinition>(StringComparer.OrdinalIgnoreCase);

        foreach (var mod in modList)
        {
            var defs = CollectLocationDefs(mod);
            modNewLocations[mod] = defs;
            foreach (var def in defs)
                allNewLocations.TryAdd(def.Id, def);
        }

        if (allNewLocations.Count == 0)
            return;

        // ── 3. Build global sorted list ────────────────────────────────────────
        var globalKeys = vanillaKeys
            .Concat(allNewLocations.Keys)
            .OrderBy(k => k, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var nameToGlobalId = globalKeys
            .Select((name, i) => (name, i))
            .ToDictionary(x => x.name, x => x.i, StringComparer.OrdinalIgnoreCase);

        // ── 4. Write merged locations.toml ─────────────────────────────────────
        InstallLocationsToml(vanillaTable, allNewLocations, reportStatus);

        // ── 5. Patch and copy each mod's TMX files ─────────────────────────────
        foreach (var mod in modList)
        {
            var tiledDir = Path.Combine(mod.GetBasePath(), "tiled");
            
            // @TODO: We should not be calling Directory.Exists here
            if (!Directory.Exists(tiledDir)) continue;

            var thisMod = modNewLocations[mod];

            // Local list: vanilla + this mod's locations sorted (what the author saw).
            var localKeys = vanillaKeys
                .Concat(thisMod.Select(d => d.Id))
                .OrderBy(k => k, StringComparer.OrdinalIgnoreCase)
                .ToList();

            var localIdToName = localKeys
                .Select((name, i) => (name, i))
                .ToDictionary(x => x.i, x => x.name);

            PatchModTmxFiles(mod, tiledDir, localIdToName, nameToGlobalId, reportStatus);
        }
    }

    // ── Private helpers ────────────────────────────────────────────────────────

    private static List<LocationDefinition> CollectLocationDefs(IMod mod)
    {
        var locDir = Path.Combine(mod.GetBasePath(), "momi", "locations");
        // @TODO: We should not be calling Directory.Exists here
        if (!Directory.Exists(locDir))
            return [];

        var defs = new List<LocationDefinition>();
        // @TODO: We shouldn't be reading with paths like this, we should be using the mod functions to account for zip mods
        foreach (var absPath in Directory.GetFiles(locDir, "*.toml", SearchOption.AllDirectories))
        {
            var content = File.ReadAllText(absPath);
            if (!string.IsNullOrWhiteSpace(content))
                defs.AddRange(LocationDefinition.ParseAll(content));
        }
        return defs;
    }

    private void InstallLocationsToml(
        TomlTable vanillaTable,
        Dictionary<string, LocationDefinition> newLocations,
        Action<string, string> reportStatus)
    {
        var merged = new TomlTable();
        MOMIOperations.MergeTomlTables(merged, vanillaTable);

        foreach (var (id, def) in newLocations)
            merged[id] = def.Data;

        var dest = Path.Combine("fiddle", "locations.toml");
        DirtyFile(dest);
        _fileModifier.Write(dest, TomlSerializer.Serialize(merged));

        reportStatus($"locations.toml: added {newLocations.Count} new location(s)", "");
    }

    private void PatchModTmxFiles(
        IMod mod,
        string tiledDir,
        Dictionary<int, string> localIdToName,
        Dictionary<string, int> nameToGlobalId,
        Action<string, string> reportStatus)
    {
        // @TODO: We should not be calling Directory.Exists here
        foreach (var absPath in Directory.GetFiles(tiledDir, "*", SearchOption.AllDirectories))
        {
            // Relative path within the mod root: "tiled\rooms\My Room\rm_my_room.tmx"
            var relPath = Path.GetRelativePath(mod.GetBasePath(), absPath);
            var dest    = Path.Combine("assets", relPath);

            if (!absPath.EndsWith(".tmx", StringComparison.OrdinalIgnoreCase))
            {
                // Non-TMX tiled asset (tileset, template, etc.) — copy verbatim.
                DirtyFile(dest);
                // @TODO: We shouldn't be reading mod files with the system File operations, it doesn't work for zip mods
                _fileModifier.Write(Path.Combine("assets", relPath), File.ReadAllText(absPath));
                // File.Copy(absPath, dest, overwrite: true);
                continue;
            }

            // @TODO: We shouldn't be reading mod files with the system File operations, it doesn't work for zip mods
            var original = File.ReadAllText(absPath);
            var patched  = PatchTmx(original, localIdToName, nameToGlobalId, out int count);

            DirtyFile(dest);
            // @TODO: Check if we need that UTF8Encoding
            _fileModifier.Write(dest, patched);
            // File.WriteAllText(dest, patched, new System.Text.UTF8Encoding(false));

            if (count > 0)
                reportStatus($"{Path.GetFileName(absPath)}: translated {count} destination_id(s)", "");
        }
    }

    private static string PatchTmx(
        string tmx,
        Dictionary<int, string> localIdToName,
        Dictionary<string, int> nameToGlobalId,
        out int replacementCount)
    {
        int count = 0;
        var result = DestinationIdRegex.Replace(tmx, match =>
        {
            int localId = int.Parse(match.Groups[2].Value);

            if (!localIdToName.TryGetValue(localId, out var locationName))
                return match.Value;

            if (!nameToGlobalId.TryGetValue(locationName, out int globalId))
                return match.Value;

            count++;
            return match.Groups[1].Value + globalId + match.Groups[3].Value;
        });

        replacementCount = count;
        return result;
    }

    private void DirtyFile(string destPath)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(destPath)!);
        if (File.Exists(destPath))
            _manifest.TrackModified(destPath);
        else
            _manifest.TrackAdded(destPath);
    }

    private static List<string> SortedLocationKeys(TomlTable table) =>
        table.Keys
             .Where(k => !k.Equals("default", StringComparison.OrdinalIgnoreCase))
             .OrderBy(k => k, StringComparer.OrdinalIgnoreCase)
             .ToList();
}
