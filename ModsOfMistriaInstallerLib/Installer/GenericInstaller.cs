using Garethp.ModsOfMistriaInstallerLib.Utils;
using Newtonsoft.Json.Linq;

namespace Garethp.ModsOfMistriaInstallerLib.Installer;

[InformationInstaller(1)]
public abstract class GenericInstaller(List<string> fileNamePaths) : IModuleInstaller
{
    private IFileModifier _fileModifier = new FileModifier();

    public void SetFileModifier(IFileModifier fileModifier) => _fileModifier = fileModifier;
    
    public void Install(
        string fieldsOfMistriaLocation,
        string modsLocation,
        GeneratedInformation information,
        Action<string, string> reportStatus
    ) {
        var fileName = fileNamePaths.Last();
        List<string> locationPath = [fieldsOfMistriaLocation];
        locationPath.AddRange(fileNamePaths[..^1]);
        var location = Path.Combine(locationPath.ToArray());
        
        var newInformation = GetNewInformation(information);
        if (_fileModifier.ConditionalRestoreBackup(location, $"{fileName}.json", () => newInformation.Count == 0)) 
            return;

        var existingInformation = JObject.Parse(_fileModifier.Read(location, $"{fileName}.json"));

        var allSources = new List<JObject> { existingInformation };
        allSources.AddRange(newInformation);

        var merged = new JObject();
        
        foreach (var source in allSources)
        {
            merged.Merge(source);
        }
        
        _fileModifier.Write(location, $"{fileName}.json", merged.ToString());
    }

    public abstract List<JObject> GetNewInformation(GeneratedInformation information);
}