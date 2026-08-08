using Garethp.ModsOfMistriaInstallerLib.ModTypes;
using Garethp.ModsOfMistriaInstallerLib.Utils;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Garethp.ModsOfMistriaInstallerLib.Installer;

// Installs .json files from a mod by merging them with the existing game files.
// JObject sources are deep-merged (keys added/overwritten, arrays unioned).
// JArray sources replace the destination array outright.
public class JSONInstaller(
    Dictionary<string, string> fileNameUidMapping,
    IFileModifier fileModifier)
    : Installer(fileNameUidMapping)
{
    public override void Install(
        IMod mod, 
        GeneratedInformation generatedInformation,
        Action<string, string> reportStatus
    ) {
        foreach (var item in generatedInformation.Json)
            InstallJson(mod, item, reportStatus);
    }
    
    private void InstallJson(IMod mod, JsonItem item, Action<string, string> reportStatus)
    {
        var content = item.ReadString(mod);
        if (string.IsNullOrEmpty(content)) return;

        var sourceToken = JToken.Parse(content);
        var dest = DestinationPath(item.FilePath);
        
        if (fileModifier.Exists(dest) && sourceToken is JObject sourceObj)
        {
            var destObj = JObject.Parse(fileModifier.Read(dest));
            destObj.Merge(sourceObj, new JsonMergeSettings
            {
                MergeArrayHandling = MergeArrayHandling.Union,
                MergeNullValueHandling = MergeNullValueHandling.Merge
            });
            fileModifier.Write(dest, destObj.ToString(Formatting.Indented));
        }
        else
        {
            fileModifier.Write(dest, sourceToken.ToString(Formatting.Indented));
        }

        reportStatus($"Installed JSON: {item.FilePath}", "");
    }
}
