using Newtonsoft.Json.Linq;

namespace Garethp.ModsOfMistriaInstaller.Installer.Generator;

public class CostumeGenerator : IGenerator
{
    private string _s;

    public GeneratedInformation Generate(string modLocation)
    {
        var information = new GeneratedInformation();
        var costumeDirectory = Path.Combine(modLocation, "costumes");

        foreach (var costumeFile in Directory.GetFiles(costumeDirectory))
        {
            var costumeJson = JObject.Parse(File.ReadAllText(costumeFile));

            foreach (var costume in costumeJson.Properties())
            {
                if (costume.Value is not JObject costumeData)
                {
                    continue;
                }
                
                if (costumeData.Property("animationFiles")?.Value is not JObject animationFiles)
                {
                    continue;
                }

                var name = costume.Name;
                
                /**
                 * @TODO:
                 * 1. Add the costume to the AssetParts list in the information object.
                 * 5. Add the sprites to UMT
                 * 6. Map ui_sub_category to the correct naming convention. IE: "back" -> "back_gear"
                 */

                var assetParts = new JObject();
                foreach (var animationType in animationFiles.Properties())
                {
                    var animationName = animationType.Name;
                    var animationData = animationType.Value;

                    if (animationData is null)
                    {
                        continue;
                    }
                    
                    assetParts.Add(animationName, $"spr_player_{name}_{animationName}");
                }
                
                var localisation = new JObject
                {
                    {
                        "eng", new JObject { { $"player_assets/{name}/name", costumeData["name"] } }
                    }
                };

                var fiddle = new JObject
                {
                    {
                        "player_assets", new JObject
                        {
                            { "name", $"player_assets/{name}/name" },
                            { "lut", $"spr_player_{name}_lut" },
                            { "ui_slot", costumeData["ui_slot"] },
                            { "default_unlocked", costumeData["default_unlocked"] },
                            { "ui_sub_category", costumeData["ui_sub_category"] }
                        }
                    }
                };
                
                var outline = new JObject
                {
                    { $"spr_ui_item_wearable_{name}", $"spr_ui_item_wearable_{name}_outline" }
                };

                information.Localisations.Add(localisation);
                information.Fiddles.Add(fiddle);
                information.Outlines.Add(outline);
                information.AssetParts.Add(new JObject
                {
                    { name, assetParts}
                });
            }
        }

        return information;
    }

    public bool CanGenerate(string modLocation) => Directory.Exists(Path.Combine(modLocation, "costumes"));
}