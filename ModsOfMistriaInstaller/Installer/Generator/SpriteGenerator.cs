using Garethp.ModsOfMistriaInstaller.Installer.UMT;
using Newtonsoft.Json.Linq;

namespace Garethp.ModsOfMistriaInstaller.Installer.Generator;

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
}