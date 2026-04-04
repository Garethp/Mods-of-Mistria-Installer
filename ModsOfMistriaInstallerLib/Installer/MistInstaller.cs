using Garethp.ModsOfMistriaInstallerLib.Utils;
using Newtonsoft.Json.Linq;

namespace Garethp.ModsOfMistriaInstallerLib.Installer;

[InformationInstaller(1)]
public class MistInstaller() : IModuleInstaller
{
    private IFileModifier _fileModifier = new FileModifier();

    public void SetFileModifier(IFileModifier fileModifier) => _fileModifier = fileModifier;

    public void Install(string fieldsOfMistriaLocation, string modsLocation, GeneratedInformation information,
        Action<string, string> reportStatus)
    {
        if (_fileModifier.ConditionalRestoreBackup(
                fieldsOfMistriaLocation,
                "__mist__.json",
                () => information.Cutscenes.Count == 0
            )) return;
        
        var existingMist = JObject.Parse(_fileModifier.Read(fieldsOfMistriaLocation, "__mist__.json"));

        var merged = new JObject();

        var allSources = new List<JObject> { existingMist };
        allSources.AddRange(information.Cutscenes);

        foreach (var source in allSources)
        {
            foreach (var function in source.Properties())
            {
                merged[function.Name] = function.Value;
            }
        }
        
        _fileModifier.Write(fieldsOfMistriaLocation, "__mist__.json", merged.ToString());
    }
}