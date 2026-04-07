using Garethp.ModsOfMistriaInstallerLib.Installer.UMT;
using Garethp.ModsOfMistriaInstallerLib.Lang;
using Garethp.ModsOfMistriaInstallerLib.Models;
using Garethp.ModsOfMistriaInstallerLib.ModTypes;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Garethp.ModsOfMistriaInstallerLib.Generator;

[InformationGenerator(1)]
public class SpriteGenerator : IGenerator
{
    public GeneratedInformation Generate(IMod mod)
    {
        var modId = mod.GetId();

        var information = new GeneratedInformation();

        foreach (var file in mod.GetFilesInFolder("sprites"))
        {
            var sprites = JsonConvert.DeserializeObject<Dictionary<string, Sprite>>(mod.ReadFile(file));
            if (sprites is null) throw new Exception($"Attempted to read file {file} but it did not match expected format.");

            foreach (var spriteName in sprites.Keys)
            {
                var sprite = sprites[spriteName];
                sprite.Name = spriteName;
                
                if (!information.Sprites.ContainsKey(modId)) information.Sprites[modId] = [];
                information.Sprites[modId].Add(new SpriteData
                {
                    Name = sprite.Name,
                    Mod = mod,
                    Location = sprite.Location,
                    IsAnimated = sprite.IsAnimated,
                    OriginX =  sprite.OriginX,
                    OriginY = sprite.OriginY,
                    MarginRight = sprite.MarginRight,
                    MarginLeft = sprite.MarginLeft,
                    MarginTop = sprite.MarginTop,
                    MarginBottom = sprite.MarginBottom,
                    BoundingBoxMode = sprite.BoundingBoxMode,
                    IsPlayerSprite = sprite.IsPlayerSprite,
                    IsUiSprite = sprite.IsUiSprite
                });
                
                if (!string.IsNullOrEmpty(sprite.OutlineLocation) && mod.FileExists(sprite.OutlineLocation))
                {
                    information.Sprites[modId].Add(new SpriteData
                    {
                        Name = $"{sprite.Name}_outline",
                        Mod = mod,
                        Location = sprite.OutlineLocation,
                        IsAnimated = false,
                        OriginX =  sprite.OriginX,
                        OriginY = sprite.OriginY,
                        MarginRight = sprite.MarginRight,
                        MarginLeft = sprite.MarginLeft,
                        MarginTop = sprite.MarginTop,
                        MarginBottom = sprite.MarginBottom,
                        BoundingBoxMode = sprite.BoundingBoxMode,
                        IsPlayerSprite = false,
                        IsUiSprite = true
                    });
                    
                    information.Outlines.Add(new JObject
                    {
                        { spriteName, $"{spriteName}_outline" }
                    });
                }
            }
        }

        return information;
    }

    public bool CanGenerate(IMod mod) => mod.HasFilesInFolder("sprites");
    
    public Validation Validate(IMod mod)
    {
        var validation = new Validation();
        if (!CanGenerate(mod)) return validation;
        
        Dictionary<string, Sprite> sprites;
        foreach (var file in mod.GetFilesInFolder("sprites"))
        {
            try
            {
                sprites = JsonConvert.DeserializeObject<Dictionary<string, Sprite>>(mod.ReadFile(file));
            }
            catch (Exception e)
            {
                validation.AddError(mod, file, string.Format(Resources.CoreCouldNotParseJSON, e.Message));
                continue;
            }
            
            if (sprites is null)
            {
                validation.AddError(mod, file, Resources.CoreNoDataInJSON);
                continue;
            }

            if (sprites.Count == 0)
            {
                validation.AddWarning(mod, file, Resources.CoreSpriteFileHasNoSprites);
            }

            foreach (var spriteName in sprites.Keys)
            {
                var sprite = sprites[spriteName];
                sprite.Name = spriteName;
                sprite.Validate(validation, mod, file);
            }
        }
        
        return validation;
    }
}