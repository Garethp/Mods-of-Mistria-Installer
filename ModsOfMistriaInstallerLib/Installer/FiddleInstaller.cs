using Garethp.ModsOfMistriaInstallerLib.Models;
using Garethp.ModsOfMistriaInstallerLib.Utils;
using Newtonsoft.Json.Linq;

namespace Garethp.ModsOfMistriaInstallerLib.Installer;

[InformationInstaller(1)]
public class FiddleInstaller : IModuleInstaller
{
    private IFileModifier _fileModifier = new FileModifier();

    public void SetFileModifier(IFileModifier fileModifier) => _fileModifier = fileModifier;

    public void Install(
        string fieldsOfMistriaLocation,
        string modsLocation,
        GeneratedInformation information,
        Action<string, string> reportStatus
    )
    {
        var existingFiddle = JObject.Parse(
            _fileModifier.Read(fieldsOfMistriaLocation, "__fiddle__.json")
        );
        
        var nestingReference = new JObject();
        var merged = new JObject();
        merged.Merge(existingFiddle);

        var modNames = information.Fiddles.Select(fiddle => fiddle.ModName).ToList();
        modNames.AddRange(information.StoreCategories.Select(category => category.ModName));
        modNames.AddRange(information.StoreItems.Select(item => item.ModName));

        modNames = modNames.Distinct().Order().ToList();

        // We want to group the installs of mods together so that fiddle file and store files install in a more
        // consistent manner, rather than them acting in an unintuitive order.
        foreach (var mod in modNames)
        {
            foreach (var fiddle in information.Fiddles.Where(fiddle => fiddle.ModName == mod).OrderBy(fiddle => fiddle.FileName))
            {
                nestingReference.Merge(fiddle.FiddleObject, new JsonMergeSettings
                {
                    MergeArrayHandling = fiddle.MergeArrayHandling,
                    MergeNullValueHandling = MergeNullValueHandling.Merge
                });
                
                merged.Merge(fiddle.FiddleObject, new JsonMergeSettings
                {
                    MergeArrayHandling = fiddle.MergeArrayHandling,
                    MergeNullValueHandling = MergeNullValueHandling.Merge
                });
            }

            var subInstallerInformation = new GeneratedInformation
            {
                StoreCategories = information.StoreCategories
                    .Where(category => category.ModName == mod)
                    .OrderBy(category => category.FileName)
                    .ToList(),
                
                StoreItems = information.StoreItems
                    .Where(item => item.ModName == mod)
                    .OrderBy(item => item.FileName)
                    .ToList()
            };

            merged = new StoreInstaller().Install(merged, subInstallerInformation, reportStatus);
        }
        
        merged = JsonNestHandler.NestTokens(merged, nestingReference);
        
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

        _fileModifier.Write(
            fieldsOfMistriaLocation, 
            "__fiddle__.json", 
            merged.ToString()
        );
    }
}