using Garethp.ModsOfMistriaInstallerLib.ModTypes;
using Garethp.ModsOfMistriaInstallerLib.Utils;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Garethp.ModsOfMistriaInstallerLib.Installer;

// Installs .json files from a mod by merging them with the existing game files.
// JObject sources are deep-merged (keys added/overwritten, arrays unioned).
// JArray sources replace the destination array outright.
public class JSONInstaller : Installer
{
    public JSONInstaller(
        string fomLocation,
        InstallManifest manifest,
        Dictionary<string, string> fileNameUIDMapping)
        : base(fomLocation, manifest, fileNameUIDMapping) { }

    public override void Install(IMod mod, Action<string, string> reportStatus)
    {
        var jsonFiles = mod.GetAllFiles(".json")
            .Where(p => !p.EndsWith(".meta.json", StringComparison.OrdinalIgnoreCase))
            .Select(p => RelativePath(mod, p))
            .Where(p => !p.Equals("manifest.json", StringComparison.OrdinalIgnoreCase))
            .Where(p => !p.StartsWith("points/", StringComparison.OrdinalIgnoreCase) &&
                        !p.StartsWith("points\\", StringComparison.OrdinalIgnoreCase))
            .ToList();

        foreach (var relPath in jsonFiles)
            InstallJson(mod, relPath, reportStatus);
    }

    private void InstallJson(IMod mod, string relPath, Action<string, string> reportStatus)
    {
        var content = mod.ReadFile(relPath);
        if (string.IsNullOrEmpty(content)) return;

        var sourceToken = JToken.Parse(content);
        var dest = DestinationPath(relPath);

        Dirty(dest);

        if (File.Exists(dest) && sourceToken is JObject sourceObj)
        {
            var destObj = JObject.Parse(File.ReadAllText(dest));
            destObj.Merge(sourceObj, new JsonMergeSettings
            {
                MergeArrayHandling = MergeArrayHandling.Union,
                MergeNullValueHandling = MergeNullValueHandling.Merge
            });
            File.WriteAllText(dest, destObj.ToString(Formatting.Indented));
        }
        else
        {
            File.WriteAllText(dest, sourceToken.ToString(Formatting.Indented));
        }

        reportStatus($"Installed JSON: {relPath}", "");
    }

    private static string RelativePath(IMod mod, string absolutePath)
    {
        var normalizedBase = mod.GetBasePath().Replace('\\', '/').TrimEnd('/') + '/';
        var normalizedFull = absolutePath.Replace('\\', '/');
        if (normalizedFull.StartsWith(normalizedBase, StringComparison.OrdinalIgnoreCase))
            return normalizedFull[normalizedBase.Length..];
        return normalizedFull;
    }
}
