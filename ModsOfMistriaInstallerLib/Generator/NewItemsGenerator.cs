using Garethp.ModsOfMistriaInstallerLib.Lang;
using Garethp.ModsOfMistriaInstallerLib.Models;
using Garethp.ModsOfMistriaInstallerLib.ModTypes;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Garethp.ModsOfMistriaInstallerLib.Generator;

[InformationGenerator(1)]
public class NewItemsGenerator: IGenerator
{
    public GeneratedInformation Generate(IMod mod)
    {
        var information = new GeneratedInformation();
        
        foreach (var file in mod.GetFilesInFolder("items"))
        {
            var newItems = JsonConvert.DeserializeObject<Dictionary<string, JObject>>(mod.ReadFile(file));
            if (newItems is null) continue;

            foreach (var itemId in newItems.Keys)
            {
                var newObject = newItems[itemId];

                var newItem = new NewItem
                {
                    Name = itemId,
                    OverwritesOtherMod = newObject["overwrites_other_mod"]?.ToObject<bool>(),
                    Data = newObject,
                };
                
                if (newObject.ContainsKey("overwrites_other_mod"))
                {
                    newObject.Remove("overwrites_other_mod");
                }

                information.NewItems.Add(newItem);
            }
        }

        return information;
    }

    public bool CanGenerate(IMod mod) => mod.HasFilesInFolder("items");

    public Validation Validate(IMod mod)
    {
        var validation = new Validation();
        if (!CanGenerate(mod)) return validation;
        
        foreach (var file in mod.GetFilesInFolder("items"))
        {
            Dictionary<string, JObject>? newItems;

            try
            {
                newItems = JsonConvert.DeserializeObject<Dictionary<string, JObject>>(mod.ReadFile(file));
            }
            catch (Exception e)
            {
                validation.AddError(mod, file, string.Format(Resources.CoreCouldNotParseJSON, e.Message));
                continue;
            }
            
            if (newItems is null)
            {
                validation.AddError(mod, file, Resources.CoreNoDataInJSON);
                continue;
            }

            if (newItems.Count == 0)
            {
                validation.AddWarning(mod, file, Resources.CoreWarningItemFileHasNoItems);
            }

            foreach (var itemName in newItems.Keys)
            {
                var item = newItems[itemName];
                
                var newItem = new NewItem
                {
                    Name = itemName,
                    OverwritesOtherMod = item["overwrites_other_mod"]?.ToObject<bool>(),
                    Data = item,
                };
                
                validation = newItem.Validate(validation, mod, file, itemName);
            }
        }

        return validation;
    }
}