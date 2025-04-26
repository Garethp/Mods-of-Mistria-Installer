using Garethp.ModsOfMistriaInstallerLib.Lang;
using Garethp.ModsOfMistriaInstallerLib.Models;
using Garethp.ModsOfMistriaInstallerLib.ModTypes;
using Newtonsoft.Json;

namespace Garethp.ModsOfMistriaInstallerLib.Generator;

[InformationGenerator(1)]
public class NewItemsGenerator: IGenerator
{
    public GeneratedInformation Generate(IMod mod)
    {
        var information = new GeneratedInformation();
        
        foreach (var file in mod.GetFilesInFolder("items"))
        {
            var newItems = JsonConvert.DeserializeObject<Dictionary<string, object>>(mod.ReadFile(file));
            if (newItems is null) continue;

            foreach (var itemId in newItems.Keys)
            {
                var newObject = newItems[itemId];
                information.NewItems.Add(new NewItem
                {
                    Name = itemId,
                    Data = newObject,
                });
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
            Dictionary<string, object>? newItems;

            try
            {
                newItems = JsonConvert.DeserializeObject<Dictionary<string, object>>(mod.ReadFile(file));
            }
            catch (Exception e)
            {
                validation.AddError(mod, file, string.Format(Resources.CouldNotParseJSON, e.Message));
                continue;
            }
            
            if (newItems is null)
            {
                validation.AddError(mod, file, Resources.NoDataInJSON);
                continue;
            }

            if (newItems.Count == 0)
            {
                validation.AddWarning(mod, file, Resources.WarningItemFileHasNoItems);
            }
        }

        return validation;
    }
}