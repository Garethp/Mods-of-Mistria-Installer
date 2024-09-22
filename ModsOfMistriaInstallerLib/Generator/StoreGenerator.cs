using Garethp.ModsOfMistriaInstallerLib.Installer.UMT;
using Garethp.ModsOfMistriaInstallerLib.Lang;
using Garethp.ModsOfMistriaInstallerLib.Models;
using Newtonsoft.Json;

namespace Garethp.ModsOfMistriaInstallerLib.Generator;

[InformationGenerator(1)]
public class StoreGenerator: IGenerator
{
    public GeneratedInformation Generate(IMod mod)
    {
        var information = new GeneratedInformation();

        foreach (var file in mod.GetFilesInFolder("stores"))
        {
            var storeFile = JsonConvert.DeserializeObject<StoreFile>(mod.ReadFile(file));
            if (storeFile is null) throw new Exception($"Attempted to read file {file} but it did not match expected format.");
            
            storeFile.Categories.ForEach(category =>
            {
                if (!information.Sprites.ContainsKey(mod.GetId())) information.Sprites[mod.GetId()] = new();
                information.Sprites[mod.GetId()].Add(new SpriteData()
                {
                    Name = category.IconName,
                    Mod = mod,
                    Location = category.Sprite,
                    IsUiSprite = true,
                });
            });
            information.StoreCategories.AddRange(storeFile.Categories);
            information.StoreItems.AddRange(storeFile.Items);
        }

        return information;
    }

    public bool CanGenerate(IMod mod) => mod.HasFilesInFolder("stores");
    
    public Validation Validate(IMod mod)
    {
        var validation = new Validation();
        if (!CanGenerate(mod)) return validation;
        
        foreach (var file in mod.GetFilesInFolder("stores").Order())
        {
            StoreFile? storeData;
            try
            {
                storeData = JsonConvert.DeserializeObject<StoreFile>(mod.ReadFile(file));
            }
            catch (Exception e)
            {
                validation.AddError(mod, file, string.Format(Resources.CouldNotParseJSON, e.Message));
                continue;
            }

            if (storeData is null)
            {
                validation.AddError(mod, file, Resources.NoDataInJSON);
                continue;
            }
            
            if (storeData.Categories.Count == 0 && storeData.Items.Count == 0)
            {
                validation.AddWarning(mod, file, Resources.StoreFileHasNoData);
            }
            
            storeData.Categories.ForEach(category =>
            {
                validation = category.Validate(validation, mod, file);
            });
        }
        
        return validation;
    }
}