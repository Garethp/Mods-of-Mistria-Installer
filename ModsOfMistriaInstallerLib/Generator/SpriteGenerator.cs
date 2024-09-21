using Garethp.ModsOfMistriaInstallerLib.Installer.UMT;
using Garethp.ModsOfMistriaInstallerLib.Lang;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Garethp.ModsOfMistriaInstallerLib.Generator;

[InformationGenerator(1)]
public class SpriteGenerator : IGenerator
{
    public GeneratedInformation Generate(Mod mod)
    {
        var modLocation = mod.Location;
        var modId = mod.Id;

        var information = new GeneratedInformation();
        var spritesDirectory = Path.Combine(mod.Location, "sprites");
        var newSprites = new List<SpriteData>();

        foreach (var file in Directory.GetFiles(spritesDirectory).Order())
        {
            var spriteInfo = JObject.Parse(File.ReadAllText(file));

            foreach (var spriteJson in spriteInfo.Properties())
            {
                if (spriteJson.Value is not JObject spriteData)
                {
                    continue;
                }

                var isAnimated = spriteData["IsAnimated"]?.Value<bool>() ?? false;
                var location = spriteData["Location"]?.Value<string>();
                if (location is null) continue;
                if (isAnimated && !Directory.Exists(Path.Combine(mod.Location, location))) continue;
                if (!isAnimated && !File.Exists(Path.Combine(mod.Location, location))) continue;
                
                var spriteName = spriteJson.Name;

                if (!information.Sprites.ContainsKey(modId)) information.Sprites[modId] = [];
                information.Sprites[modId].Add(new SpriteData
                {
                    Name = spriteName,
                    BaseLocation = modLocation,
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
            }
        }

        return information;
    }

    public bool CanGenerate(Mod mod)
    {
        return Directory.Exists(Path.Combine(mod.Location, "sprites"));
    }
    
    public Validation Validate(Mod mod)
    {
        var validation = new Validation();
        if (!CanGenerate(mod)) return validation;
        
        Dictionary<string, SpriteData> sprites;
        foreach (var file in Directory.GetFiles(Path.Combine(mod.Location, "sprites")))
        {
            try
            {
                sprites = JsonConvert.DeserializeObject<Dictionary<string, SpriteData>>(File.ReadAllText(file));
            } 
            catch (Exception e)
            {
                validation.AddError(mod, file, string.Format(Resources.CouldNotParseJSON, e.Message));
                continue;
            }
            
            if (sprites is null)
            {
                validation.AddError(mod, file, Resources.NoDataInJSON);
                continue;
            }

            if (sprites.Count == 0)
            {
                validation.AddWarning(mod, file, Resources.SpriteFileHasNoSprites);
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