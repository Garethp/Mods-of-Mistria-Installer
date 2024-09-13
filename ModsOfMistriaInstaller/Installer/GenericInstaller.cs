using Newtonsoft.Json.Linq;

namespace Garethp.ModsOfMistriaInstaller.Installer;

public abstract class GenericInstaller(List<string> fileNamePaths) : IModuleInstaller
{
    public void Install(string fieldsOfMistriaLocation, GeneratedInformation information, Action<string, string> reportStatus)
    {
        var fileName = fileNamePaths.Last();
        List<string> locationPath = [fieldsOfMistriaLocation];
        locationPath.AddRange(fileNamePaths[..^1]);
        var location = Path.Combine(locationPath.ToArray());
        
        if (!File.Exists(Path.Combine(location, $"{fileName}.json")))
        {
            throw new FileNotFoundException($"Could not find {fileName}.json in Fields of Mistria folder");
        }

        if (!File.Exists(Path.Combine(location, $"{fileName}.bak.json")))
        {
            File.Copy(
                Path.Combine(location, $"{fileName}.json"),
                Path.Combine(location, $"{fileName}.bak.json")
            );
        }
        
        var newInformation = GetNewInformation(information);
        if (newInformation.Count == 0) return;
        
        var existingInformation = JObject.Parse(
            File.ReadAllText(Path.Combine(location, $"{fileName}.bak.json"))
        );

        var allSources = new List<JObject> { existingInformation };
        allSources.AddRange(newInformation);

        var merged = new JObject();
        
        foreach (var source in allSources)
        {
            merged.Merge(source);
        }
        
        File.WriteAllText(
            Path.Combine(location, $"{fileName}.json"),
            merged.ToString()
        );
    }

    public abstract List<JObject> GetNewInformation(GeneratedInformation information);
}