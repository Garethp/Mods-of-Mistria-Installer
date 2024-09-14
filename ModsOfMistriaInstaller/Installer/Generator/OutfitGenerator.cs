using Garethp.ModsOfMistriaInstaller.Installer.UMT;
using Newtonsoft.Json.Linq;

namespace Garethp.ModsOfMistriaInstaller.Installer.Generator;

public class OutfitGenerator : IGenerator
{
    public GeneratedInformation Generate(Mod mod)
    {
        var modLocation = mod.Location;
        var modId = mod.Id;

        // @TODO: Remove the images here so that we can store them in whatever folders we want
        var basePath = modLocation;

        var information = new GeneratedInformation();
        var outfitsDirectory = Path.Combine(modLocation, "outfits");

        foreach (var outfitFile in Directory.GetFiles(outfitsDirectory))
        {
            var outfitJson = JObject.Parse(File.ReadAllText(outfitFile));

            foreach (var outfit in outfitJson.Properties())
            {
                var newSprites = new List<SpriteData>();

                if (outfit.Value is not JObject outfitData)
                {
                    continue;
                }

                if (outfitData.Property("animationFiles")?.Value is not JObject animationFiles)
                {
                    continue;
                }

                var name = outfit.Name;

                /**
                 * @TODO:
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
                        IsAnimated = true,
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
                        "eng", new JObject { { $"player_assets/{name}/name", outfitData["name"] } }
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
                                    { "ui_slot", outfitData["ui_slot"] },
                                    { "default_unlocked", outfitData["default_unlocked"] },
                                    { "ui_sub_category", outfitData["ui_sub_category"] }
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
                        Location = outfitData["lutFile"].ToString(),
                        IsAnimated = false,
                        IsPlayerSprite = true,
                    },
                    new()
                    {
                        Name = $"spr_ui_item_wearable_{name}",
                        BaseLocation = basePath,
                        Location = outfitData["uiItem"].ToString(),
                        IsAnimated = false,
                        IsUiSprite = true,
                    },
                    new()
                    {
                        Name = $"spr_ui_item_wearable_{name}_outline",
                        BaseLocation = basePath,
                        Location = outfitData["outlineFile"].ToString(),
                        IsAnimated = false,
                        IsUiSprite = true,
                    }
                ]);

                if (!information.Sprites.ContainsKey(modId)) information.Sprites[modId] = [];
                information.Sprites[modId].AddRange(newSprites);

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

    public bool CanGenerate(Mod mod) => Directory.Exists(Path.Combine(mod.Location, "outfits"));
}