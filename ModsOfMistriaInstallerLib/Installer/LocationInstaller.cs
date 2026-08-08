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
    private readonly IFileModifier _fileModifier;

    private static readonly Regex DestinationIdRegex = new(
        @"(<property\s+name=""destination_id""\s+type=""int""\s+propertytype=""LocationId""\s+value="")(\d+)("")",
        RegexOptions.Compiled);

    public LocationInstaller(string fomLocation, IFileModifier fileModifier)
    {
        _assetsLocation = Path.Combine(fomLocation, "assets");
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
            var tiledFiles = GetFilesUnder(mod, "tiled/");
            if (tiledFiles.Count == 0) continue;

            var thisMod = modNewLocations[mod];

            // Local list: vanilla + this mod's locations sorted (what the author saw).
            var localKeys = vanillaKeys
                .Concat(thisMod.Select(d => d.Id))
                .OrderBy(k => k, StringComparer.OrdinalIgnoreCase)
                .ToList();

            var localIdToName = localKeys
                .Select((name, i) => (name, i))
                .ToDictionary(x => x.i, x => x.name);

            PatchModTmxFiles(mod, tiledFiles, localIdToName, nameToGlobalId, reportStatus);
        }
    }

    // ── Private helpers ────────────────────────────────────────────────────────

    private static List<LocationDefinition> CollectLocationDefs(IMod mod)
    {
        var defs = new List<LocationDefinition>();
        foreach (var path in GetFilesUnder(mod, "momi/locations/"))
        {
            if (!path.EndsWith(".toml", StringComparison.OrdinalIgnoreCase)) continue;
            var content = mod.ReadFile(path);
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

        var dest = Path.Combine("assets", "fiddle", "locations.toml");
        _fileModifier.Write(dest, TomlSerializer.Serialize(merged));

        reportStatus($"locations.toml: added {newLocations.Count} new location(s)", "");
    }

    private void PatchModTmxFiles(
        IMod mod,
        IReadOnlyList<string> tiledFiles,
        Dictionary<int, string> localIdToName,
        Dictionary<string, int> nameToGlobalId,
        Action<string, string> reportStatus)
    {
        foreach (var path in tiledFiles)
        {
            var relPath = RelativePath(mod, path);
            var dest = Path.Combine("assets", relPath.Replace('/', Path.DirectorySeparatorChar));

            if (!relPath.EndsWith(".tmx", StringComparison.OrdinalIgnoreCase))
            {
                // Non-TMX tiled asset (tileset, template, etc.) — copy verbatim.
                using var source = mod.ReadFileAsStream(relPath);
                using var buffer = new MemoryStream();
                source.CopyTo(buffer);
                _fileModifier.Write(dest, buffer.ToArray());
                continue;
            }

            var original = mod.ReadFile(relPath);
            var patched  = PatchTmx(original, localIdToName, nameToGlobalId, out int count);

            // @TODO: Check if we need that UTF8Encoding
            _fileModifier.Write(dest, patched);
            // File.WriteAllText(dest, patched, new System.Text.UTF8Encoding(false));

            if (count > 0)
                reportStatus($"{Path.GetFileName(relPath)}: translated {count} destination_id(s)", "");
        }
    }

    private static List<string> GetFilesUnder(IMod mod, string directory)
    {
        var prefix = directory.Replace('\\', '/').TrimStart('/');
        if (!prefix.EndsWith('/')) prefix += "/";

        return mod.GetAllFiles("")
            .Where(path => RelativePath(mod, path).StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            .ToList();
    }

    private static string RelativePath(IMod mod, string path)
    {
        var normalizedPath = path.Replace('\\', '/');
        var normalizedBase = mod.GetBasePath().Replace('\\', '/').TrimEnd('/');

        if (!string.IsNullOrEmpty(normalizedBase) &&
            normalizedPath.StartsWith(normalizedBase + "/", StringComparison.OrdinalIgnoreCase))
            return normalizedPath[(normalizedBase.Length + 1)..];

        return normalizedPath.TrimStart('/');
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

    private static List<string> SortedLocationKeys(TomlTable table) =>
        table.Keys
             .Where(k => !k.Equals("default", StringComparison.OrdinalIgnoreCase))
             .OrderBy(k => k, StringComparer.OrdinalIgnoreCase)
             .ToList();
}
