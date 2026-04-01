using Garethp.ModsOfMistriaInstallerLib.Installer.UMT;
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
            var outfitJson = JsonConvert.DeserializeObject<Dictionary<string, Outfit>>(mod.ReadFile(outfitFile));
            if (outfitJson is null) throw new Exception($"Attempted to read file {outfitFile} but it did not match expected format.");
            
            foreach (var outfitId in outfitJson.Keys)
            {
                var outfit = outfitJson[outfitId];
                
                var newSprites = new List<SpriteData>();

                var name = outfitId;

                /**
                 * @TODO:
                 * 6. Map ui_sub_category to the correct naming convention. IE: "back" -> "back_gear"
                 */

                var assetParts = new JObject();
                foreach (var animationName in outfit.AnimationFiles.Keys)
                {
                    var animationData = outfit.AnimationFiles[animationName];

                    // @TODO: Handle trailing slashes in the animationData string
                    newSprites.Add(new()
                    {
                        Name = $"spr_player_{name}_{animationName}",
                        Mod = mod,
                        Location = animationData,
                        IsAnimated = true,
                        BoundingBoxMode = 1,
                        DeleteCollisionMask = true,
                        SpecialType = true,
                        SpecialTypeVersion = 3,
                        SpecialPlaybackSpeed = 40,
                        IsPlayerSprite = true
                    });
                    assetParts.Add(animationName, $"spr_player_{name}_{animationName}");
                }

                var localisation = new JObject
                {
                    {
                        "eng", new JObject { { $"player_assets/{name}/name", outfit.Name } }
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
                                    { "ui_slot", outfit.UiSlot },
                                    { "default_unlocked", outfit.DefaultUnlocked },
                                    { "ui_sub_category", outfit.UiSubCategory }
                                }
                            }
                        }
                    }
                };

                if (outfit.PriceOverride is not null)
                {
                    (fiddle["player_assets"][name] as JObject).Add("price_override", outfit.PriceOverride);
                }

                // handle outline as _merged and _merged_outline for face cosmetics
                var isFaceCosmetic = outfit.IsFaceCosmetic;
                
                var outline = new JObject
                {
                    { $"spr_ui_item_wearable_{name}", $"spr_ui_item_wearable_{name}_outline" }
                };
                if (isFaceCosmetic)
                {
                    outline = new JObject
                    {
                        { $"spr_ui_item_wearable_{name}_merged", $"spr_ui_item_wearable_{name}_merged_outline" }
                    };
                }
                // add lut
                newSprites.Add(new()
                {
                    Name = $"spr_player_{name}_lut",
                    Mod = mod,
                    Location = outfit.LutFile,
                    IsAnimated = false,
                    IsPlayerSprite = true
                });

                // add ui sprites
                if (!isFaceCosmetic)
                {
                    newSprites.AddRange([
                        new()
                        {
                            Name = $"spr_ui_item_wearable_{name}",
                            Mod = mod,
                            Location = outfit.UiItem,
                            IsAnimated = false,
                            IsUiSprite = true
                        },
                        new()
                        {
                            Name = $"spr_ui_item_wearable_{name}_outline",
                            Mod = mod,
                            Location = outfit.OutlineFile,
                            IsAnimated = false,
                            IsUiSprite = true
                        }
                    ]);
                }
                else
                {
                    newSprites.AddRange([
                        new()
                        {
                            Name = $"spr_ui_item_wearable_{name}_asset",
                            Mod = mod,
                            Location = outfit.UiAssetFile,
                            IsAnimated = false,
                            IsUiSprite = true
                        },
                        new()
                        {
                            Name = $"spr_ui_item_wearable_{name}_body",
                            Mod = mod,
                            Location = outfit.UiBodyFile,
                            IsAnimated = false,
                            IsUiSprite = true
                        },
                        new()
                        {
                            Name = $"spr_ui_item_wearable_{name}_merged",
                            Mod = mod,
                            Location = outfit.UiItem,
                            IsAnimated = false,
                            IsUiSprite = true
                        },
                        new()
                        {
                            Name = $"spr_ui_item_wearable_{name}_merged_outline",
                            Mod = mod,
                            Location = outfit.OutlineFile,
                            IsAnimated = false,
                            IsUiSprite = true
                        }
                    ]);
                }

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
                validation.AddError(mod, file, string.Format(Resources.CoreCouldNotParseJSON, e.Message));
                continue;
            }
            
            if (outfits is null)
            {
                validation.AddError(mod, file, Resources.CoreNoDataInJSON);
                continue;
            }

            if (outfits.Count == 0)
            {
                validation.AddWarning(mod, file, Resources.CoreOutfitFileHasNoOutfits);
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