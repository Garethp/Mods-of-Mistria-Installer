using Garethp.ModsOfMistriaInstallerLib.Installer.UMT;
using Garethp.ModsOfMistriaInstallerLib.Lang;
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
            var spriteInfo = JObject.Parse(mod.ReadFile(file));

            foreach (var spriteJson in spriteInfo.Properties())
            {
                if (spriteJson.Value is not JObject spriteData)
                {
                    continue;
                }

                var isAnimated = spriteData["IsAnimated"]?.Value<bool>() ?? false;
                var location = spriteData["Location"]?.Value<string>();
                if (location is null) continue;
                if (isAnimated && !mod.FolderExists(location)) continue;
                if (!isAnimated && !mod.FileExists(location)) continue;
                
                var spriteName = spriteJson.Name;

                if (!information.Sprites.ContainsKey(modId)) information.Sprites[modId] = [];
                information.Sprites[modId].Add(new SpriteData
                {
                    Name = spriteName,
                    Mod = mod,
                    Location = location,
                    IsAnimated = isAnimated,
                    OriginX =  spriteData["OriginX"]?.Value<int>(),
                    OriginY = spriteData["OriginY"]?.Value<int>(),
                    MarginRight = spriteData["MarginRight"]?.Value<int>(),
                    MarginLeft = spriteData["MarginLeft"]?.Value<int>(),
                    MarginTop = spriteData["MarginTop"]?.Value<int>(),
                    MarginBottom = spriteData["MarginBottom"]?.Value<int>(),
                    BoundingBoxMode = spriteData["BoundingBoxMode"]?.Value<uint>(),
                    IsPlayerSprite = spriteData["IsPlayerSprite"]?.Value<bool>() ?? false,
                    IsUiSprite = spriteData["IsUiSprite"]?.Value<bool>() ?? false
                });
                
                var outline = spriteData["OutlineLocation"]?.Value<string>();
                if (!string.IsNullOrEmpty(outline) && mod.FileExists(outline))
                {
                    information.Sprites[modId].Add(new SpriteData
                    {
                        Name = $"{spriteName}_outline",
                        Mod = mod,
                        Location = outline,
                        IsAnimated = false,
                        OriginX =  spriteData["OriginX"]?.Value<int>(),
                        OriginY = spriteData["OriginY"]?.Value<int>(),
                        MarginRight = spriteData["MarginRight"]?.Value<int>(),
                        MarginLeft = spriteData["MarginLeft"]?.Value<int>(),
                        MarginTop = spriteData["MarginTop"]?.Value<int>(),
                        MarginBottom = spriteData["MarginBottom"]?.Value<int>(),
                        BoundingBoxMode = spriteData["BoundingBoxMode"]?.Value<uint>(),
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
        
        Dictionary<string, SpriteData> sprites;
        foreach (var file in mod.GetFilesInFolder("sprites"))
        {
            try
            {
                sprites = JsonConvert.DeserializeObject<Dictionary<string, SpriteData>>(mod.ReadFile(file));
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