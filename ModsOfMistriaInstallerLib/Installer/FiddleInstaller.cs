using Newtonsoft.Json.Linq;

namespace Garethp.ModsOfMistriaInstallerLib.Installer;

[InformationInstaller(1)]
public class FiddleInstaller : IModuleInstaller
{
    public void Install(
        string fieldsOfMistriaLocation,
        string modsLocation,
        GeneratedInformation information,
        Action<string, string> reportStatus
    ) {
        if (!File.Exists(Path.Combine(fieldsOfMistriaLocation, "__fiddle__.json")))
        {
            throw new FileNotFoundException("Could not find __fiddle__.json in Fields of Mistria folder");
        }

        if (!File.Exists(Path.Combine(fieldsOfMistriaLocation, "__fiddle__.bak.json")))
        {
            File.Copy(
                Path.Combine(fieldsOfMistriaLocation, "__fiddle__.json"),
                Path.Combine(fieldsOfMistriaLocation, "__fiddle__.bak.json")
            );
        }
        
        var existingFiddle = JObject.Parse(
            File.ReadAllText(Path.Combine(fieldsOfMistriaLocation, "__fiddle__.bak.json"))
        );

        existingFiddle = new StoreInstaller().Install(existingFiddle, information, reportStatus);
        
        var allSources = new List<JObject> { existingFiddle };
        
        // @TODO: Scramble the JSON here
        allSources.AddRange(information.Fiddles);

        var merged = new JObject();
        
        foreach (var source in allSources)
        {
            merged.Merge(source, new JsonMergeSettings
            {
                MergeArrayHandling = MergeArrayHandling.Merge,
                MergeNullValueHandling = MergeNullValueHandling.Merge
            });
        }
        
        File.WriteAllText(
            Path.Combine(fieldsOfMistriaLocation, "__fiddle__.json"),
            merged.ToString()
        );
    }
}