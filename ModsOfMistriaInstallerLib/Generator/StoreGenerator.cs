﻿using Garethp.ModsOfMistriaInstallerLib.Installer.UMT;
using Garethp.ModsOfMistriaInstallerLib.Models;
using Newtonsoft.Json;

namespace Garethp.ModsOfMistriaInstallerLib.Generator;

[InformationGenerator(1)]
public class StoreGenerator: IGenerator
{
    public GeneratedInformation Generate(Mod mod)
    {
        var information = new GeneratedInformation();

        foreach (var file in Directory.GetFiles(Path.Combine(mod.Location, "stores")).Order())
        {
            var storeFile = JsonConvert.DeserializeObject<StoreFile>(File.ReadAllText(file));
            if (storeFile is null) throw new Exception($"Attempted to read file {file} but it did not match expected format.");
            
            storeFile.Categories.ForEach(category =>
            {
                if (!information.Sprites.ContainsKey(mod.Id)) information.Sprites[mod.Id] = new();
                information.Sprites[mod.Id].Add(new SpriteData()
                {
                    Name = category.IconName,
                    BaseLocation = mod.Location,
                    Location = category.Sprite,
                    IsUiSprite = true,
                });
            });
            information.StoreCategories.AddRange(storeFile.Categories);
            information.StoreItems.AddRange(storeFile.Items);
        }

        return information;
    }

    public bool CanGenerate(Mod mod) => Directory.Exists(Path.Combine(mod.Location, "stores"));
    
    public Validation Validate(Mod mod)
    {
        var validation = new Validation();
        if (!CanGenerate(mod)) return validation;
        
        foreach (var file in Directory.GetFiles(Path.Combine(mod.Location, "stores")).Order())
        {
            StoreFile? storeData;
            try
            {
                storeData = JsonConvert.DeserializeObject<StoreFile>(File.ReadAllText(file));
            }
            catch (Exception e)
            {
                validation.AddError(mod, file, $"Could not parse file with message: {e.Message}");
                continue;
            }

            if (storeData is null)
            {
                validation.AddError(mod, file, $"No data was found in file");
                continue;
            }
            
            if (storeData.Categories.Count == 0 && storeData.Items.Count == 0)
            {
                validation.AddWarning(mod, file, "Store file has no categories or items.");
            }
            
            storeData.Categories.ForEach(category =>
            {
                validation = category.Validate(validation, mod, file);
            });
        }
        
        return validation;
    }
}