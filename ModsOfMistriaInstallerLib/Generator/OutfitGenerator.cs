using Garethp.ModsOfMistriaInstallerLib.Installer.UMT;
using Garethp.ModsOfMistriaInstallerLib.Lang;
using Garethp.ModsOfMistriaInstallerLib.Lang;
using Garethp.ModsOfMistriaInstallerLib.Models;
using Garethp.ModsOfMistriaInstallerLib.ModTypes;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Garethp.ModsOfMistriaInstallerLib.Generator;

[InformationGenerator(1)]
public class OutfitGenerator : IGenerator
{
    public GeneratedInformation Generate(IMod mod)
    {
        var modId = mod.GetId();

        // @TODO: Remove the images here so that we can store them in whatever folders we want
        var information = new GeneratedInformation();

        foreach (var outfitFile in mod.GetFilesInFolder("outfits").Order())
        {
            var outfitJson = JObject.Parse(mod.ReadFile(outfitFile));

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
                        Mod = mod,
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
                        Mod = mod,
                        Location = outfitData["lutFile"].ToString(),
                        IsAnimated = false,
                        IsPlayerSprite = true,
                    },
                    new()
                    {
                        Name = $"spr_ui_item_wearable_{name}",
                        Mod = mod,
                        Location = outfitData["uiItem"].ToString(),
                        IsAnimated = false,
                        IsUiSprite = true,
                    },
                    new()
                    {
                        Name = $"spr_ui_item_wearable_{name}_outline",
                        Mod = mod,
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

    public bool CanGenerate(IMod mod) => mod.HasFilesInFolder("outfits");
    
    public Validation Validate(IMod mod)
    {
        var validation = new Validation();
        if (!CanGenerate(mod)) return validation;

        foreach (var file in mod.GetFilesInFolder("outfits"))
        {
            Dictionary<string, Outfit>? outfits;
            try
            {
                outfits = JsonConvert.DeserializeObject<Dictionary<string, Outfit>>(mod.ReadFile(file));
            }
            catch (Exception e)
            {
                validation.AddError(mod, file, string.Format(Resources.CouldNotParseJSON, e.Message));
                continue;
            }
            
            if (outfits is null)
            {
                validation.AddError(mod, file, Resources.NoDataInJSON);
                continue;
            }

            if (outfits.Count == 0)
            {
                validation.AddWarning(mod, file, Resources.OutfitFileHasNoOutfits);
            }

            foreach (var outfitName in outfits.Keys)
            {
                var outfit = outfits[outfitName];
                validation = outfit.Validate(validation, mod, file, outfitName);
            }
        }

        return validation;
    }
}