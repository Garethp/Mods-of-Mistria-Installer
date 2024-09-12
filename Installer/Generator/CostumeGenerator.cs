using Garethp.ModsOfMistriaInstaller.Installer.UMT;
using Newtonsoft.Json.Linq;

namespace Garethp.ModsOfMistriaInstaller.Installer.Generator;

public class CostumeGenerator : IGenerator
{
    private string _s;

    public GeneratedInformation Generate(string modLocation)
    {
        // @TODO: Fetch the name from a manifest file
        var modName = "olrics_love";

        // @TODO: Remove the images here so that we can store them in whatever folders we want
        var basePath = Path.Combine(modLocation, "images");

        var information = new GeneratedInformation();
        var costumeDirectory = Path.Combine(modLocation, "costumes");
        var newSprites = new List<SpriteData>();

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

                    // @TODO: Handle trailing slashes in the animationData string
                    newSprites.Add(new()
                    {
                        Name = $"spr_player_{name}_{animationName}",
                        BaseLocation = basePath,
                        Location = animationData.ToString(),
                        HasFrames = true,
                        BoundingBoxMode = 1,
                        DeleteCollisionMask = true,
                        SpecialType = true,
                        SpecialTypeVersion = 3,
                        SpecialPlaybackSpeed = 40,
                        IsPlayerSprite = true,
                    });
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
                            {
                                $"{name}", new JObject
                                {
                                    { "name", $"player_assets/{name}/name" },
                                    { "lut", $"spr_player_{name}_lut" },
                                    { "ui_slot", costumeData["ui_slot"] },
                                    { "default_unlocked", costumeData["default_unlocked"] },
                                    { "ui_sub_category", costumeData["ui_sub_category"] }
                                }
                            }
                        }
                    }
                };

                var outline = new JObject
                {
                    { $"spr_ui_item_wearable_{name}", $"spr_ui_item_wearable_{name}_outline" }
                };

                newSprites.AddRange([
                    new()
                    {
                        Name = $"spr_player_{name}_lut",
                        BaseLocation = basePath,
                        Location = costumeData["lutFile"].ToString(),
                        HasFrames = false,
                        IsPlayerSprite = true,
                    },
                    new()
                    {
                        Name = $"spr_ui_item_wearable_{name}",
                        BaseLocation = basePath,
                        Location = costumeData["uiItem"].ToString(),
                        HasFrames = false,
                        IsUiSprite = true,
                    },
                    new()
                    {
                        Name = $"spr_ui_item_wearable_{name}_outline",
                        BaseLocation = basePath,
                        Location = costumeData["outlineFile"].ToString(),
                        HasFrames = false,
                        IsUiSprite = true,
                    }
                ]);

                if (!information.Sprites.ContainsKey(modName)) information.Sprites[modName] = [];
                information.Sprites[modName].AddRange(newSprites);

                information.Localisations.Add(localisation);
                information.Fiddles.Add(fiddle);
                information.Outlines.Add(outline);
                information.AssetParts.Add(new JObject
                {
                    { name, assetParts }
                });
            }
        }

        return information;
    }

    public bool CanGenerate(string modLocation) => Directory.Exists(Path.Combine(modLocation, "costumes"));
}