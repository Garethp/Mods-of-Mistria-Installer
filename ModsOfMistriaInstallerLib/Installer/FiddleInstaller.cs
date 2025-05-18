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

        if (merged["extras"] is not JObject)
        {
            merged["extras"] = new JObject()
            {
                { "objects", new JArray() },
                { "items", new JArray() }
            };
        }
        
        if (merged["extras"]!["objects"] is not JArray) merged["extras"]!["objects"] = new JArray();
        if (merged["extras"]!["items"] is not JArray) merged["extras"]!["items"] = new JArray();

        var extraObjects = (merged["extras"]!["objects"] as JArray)!;

        var mergedNewObjects = new Dictionary<string, JObject>();
        foreach (var newObject in information.NewObjects)
        {
            if (!mergedNewObjects.ContainsKey(newObject.Name))
            {
                mergedNewObjects.Add(newObject.Name, JObject.FromObject(newObject));
            }
            else
            {
                mergedNewObjects[newObject.Name].Merge(JObject.FromObject(newObject));
            }
        }
        
        foreach (var newObject in mergedNewObjects.Values)
        {
            extraObjects.Add(newObject);
        }
        
        merged["extras/objects"] = merged["extras"]!["objects"];
        
        var extraItems = (merged["extras"]!["items"] as JArray)!;

        var mergedNewItems = new Dictionary<string, JObject>();
        foreach (var newItem in information.NewItems)
        {
            if (!mergedNewItems.ContainsKey(newItem.Name))
            {
                mergedNewItems.Add(newItem.Name, JObject.FromObject(newItem));
            }
            else
            {
                mergedNewItems[newItem.Name].Merge(JObject.FromObject(newItem));
            }
        }
        
        foreach (var newItem in mergedNewItems.Values)
        {
            extraItems.Add(newItem);
        }

        merged["extras/items"] = merged["extras"]!["items"];
        
        File.WriteAllText(
            Path.Combine(fieldsOfMistriaLocation, "__fiddle__.json"),
            merged.ToString()
        );
    }
}