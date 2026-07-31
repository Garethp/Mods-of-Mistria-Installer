using Garethp.ModsOfMistriaInstallerLib.Collector;
using Garethp.ModsOfMistriaInstallerLib.Models.MOMI;
using Garethp.ModsOfMistriaInstallerLib.Models.SDK;
using Garethp.ModsOfMistriaInstallerLib.ModTypes;
using Garethp.ModsOfMistriaInstallerLib.Utils;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SixLabors.ImageSharp;
using Tomlyn;
using Tomlyn.Model;

namespace Garethp.ModsOfMistriaInstallerLib.Generator;

public class CosmeticGenerator
{
    public GeneratedInformation Generate(IMod mod)
    {
        var information = new GeneratedInformation();

        foreach (var file in mod.GetFilesInFolder("momi/cosmetic", ".toml"))
        {
            var content = mod.ReadFile(file);
            if (string.IsNullOrEmpty(content)) continue;
            
            var cosmetics = TomlSerializer.Deserialize<Dictionary<string, CosmeticFile>>(content);
            if (cosmetics is null) continue;
            
            foreach (var id in cosmetics.Keys)
            {
                var cosmetic = cosmetics[id];
                cosmetic.Id = id;
                
                information.Merge(GenerateFiles(mod, cosmetic));
            }
        }
        
        return information;
    }

    private GeneratedInformation GenerateFiles(IMod mod, CosmeticFile cosmetic)
    {
        var information = new GeneratedInformation();
        var playerAssetParts = new JObject();

        foreach (var part in cosmetic.AnimationFiles.Keys)
        {
            var partFile = cosmetic.AnimationFiles[part];
            var frameCount = cosmetic.GetPartFrameCount(part);
            var (width, height) = DetectSize(mod, partFile, frameCount);

            information.AnimationGroups[$"{cosmetic.Id}_{part}"] = new AnimationGroup
            {
                BaseName = $"{cosmetic.Id}_{part}",
                PngRelPath = partFile,
                AnimationMetaRelPath = new GeneratedTomlItem
                {
                    FilePath = $"animations/{mod.GetId()}/{cosmetic.Id}/spr_{cosmetic.Id}_{part}.meta.toml",
                    Contents = AnimationMeta(width, height, frameCount, "Player", 0.025f, "Left", "Top")
                },
                ShapeMetaRelPath = new GeneratedTomlItem
                {
                    FilePath = $"shapes/{mod.GetId()}/{cosmetic.Id}/poly_{cosmetic.Id}_{part}.meta.toml",
                    Contents = ShapeMeta(width, height, 0, 0)
                }
            };

            playerAssetParts[part] = $"spr_{cosmetic.Id}_{part}";
        }
        
        var lutSprite = "";
        if (!string.IsNullOrEmpty(cosmetic.LutFile))
        {
            lutSprite = $"spr_{cosmetic.Id}_lut";
            var (lutWidth, lutHeight) = DetectSize(mod, cosmetic.LutFile, 1);

            information.AnimationGroups[$"{cosmetic.Id}_lut"] = new AnimationGroup
            {
                BaseName = $"{cosmetic.Id}_lut",
                PngRelPath = cosmetic.LutFile,
                AnimationMetaRelPath = new GeneratedTomlItem
                {
                    FilePath = $"animations/{mod.GetId()}/{cosmetic.Id}/{lutSprite}.meta.toml",
                    Contents = AnimationMeta(lutWidth, lutHeight, 1, "Player", 0f, "Middle", "Middle")
                },
                ShapeMetaRelPath = new GeneratedTomlItem
                {
                    FilePath = $"shapes/{mod.GetId()}/{cosmetic.Id}/poly_{cosmetic.Id}_lut.meta.toml",
                    Contents = ShapeMeta(lutWidth, lutHeight, 0, 0)
                }
            };
        } else if (!string.IsNullOrEmpty(cosmetic.LutSprite))
        {
            lutSprite = cosmetic.LutSprite;
        }
        else
        {
            // @TODO: Throw an error here
        }
        
        Dictionary<string, string> outlineFiles;
        if (cosmetic.IsSimpleIcon())
        {
            outlineFiles = new Dictionary<string, string>
            {
                { $"ui_item_wearable_{cosmetic.Id}", cosmetic.UiFile! },
                { $"{cosmetic.Id}_outline", cosmetic.OutlineFile! }
            };
            
            information.Json.Add(new JsonItem
            {
                FilePath = "data_files/animation/outlines.json",
                Contents = new JObject
                {
                    { $"spr_ui_item_wearable_{cosmetic.Id}", $"spr_{cosmetic.Id}_outline" }
                }.ToString(Formatting.None)
            });
        }
        else
        {
            outlineFiles = new Dictionary<string, string>
            {
                { $"ui_item_wearable_{cosmetic.Id}_asset", cosmetic.AssetFile! },
                { $"ui_item_wearable_{cosmetic.Id}_body", cosmetic.BodyFile! },
                { $"ui_item_wearable_{cosmetic.Id}_merged", cosmetic.MergedFile! },
                { $"ui_item_wearable_{cosmetic.Id}_merged_outline", cosmetic.MergedOutlineFile! }
            };
            
            information.Json.Add(new JsonItem
            {
                FilePath = "data_files/animation/outlines.json",
                Contents = new JObject
                {
                    { $"spr_ui_item_wearable_{cosmetic.Id}_merged", $"spr_ui_item_wearable_{cosmetic.Id}_merged_outline" }
                }.ToString(Formatting.None)
            });
        }

        foreach (var id in outlineFiles.Keys)
        {
            var file = outlineFiles[id];
            
            information.AnimationGroups[id] = new AnimationGroup
            {
                BaseName = id,
                PngRelPath = file,
                AnimationMetaRelPath = new GeneratedTomlItem
                {
                    FilePath = $"animations/{mod.GetId()}/{cosmetic.Id}/spr_{id}.meta.toml",
                    Contents = AnimationMeta(18, 18, 1, "UI", 0f, 9.0, 9.0)
                },
                ShapeMetaRelPath = new GeneratedTomlItem
                {
                    FilePath = $"shapes/{mod.GetId()}/{cosmetic.Id}/poly_{id}.meta.toml",
                    Contents = ShapeMeta(18, 18, -9, -9)
                }
            };
        }

        var playerAssets = new TomlTable
        {
            { "name", cosmetic.Name },
            { "lut", lutSprite },
            { "ui_slot", cosmetic.UiSlot },
            { "ui_sub_category", cosmetic.UiSubCategory },
        };

        if (cosmetic.DefaultUnlocked is not null)
            playerAssets["default_unlocked"] = cosmetic.DefaultUnlocked;
        
        if (cosmetic.PriceOverride is not null)
            playerAssets["price_override"] = cosmetic.PriceOverride;
        
        information.Toml.Add(new GeneratedTomlItem
        {
            FilePath = "fiddle/player_assets.toml",
            Contents = TomlSerializer.Serialize(new TomlTable
            {
                { cosmetic.Id, playerAssets }
            })
        });
        
        information.Json.Add(new JsonItem
        {
            FilePath = "data_files/animation/player_asset_parts.json",
            Contents = new JObject
            {
                { cosmetic.Id, playerAssetParts }
            }.ToString(Formatting.None)
        });

        return information;
    }
    
    private static string AnimationMeta(
        int frameWidth, int frameHeight, int frameCount,
        string atlas, float duration,
        object offsetH, object offsetV)
    {
        var animation = new SpriteMetaFile
        {
            Meta = new SpriteMetaFileProperties
            {
                Id = IDManager.GenerateUniqueId(),
                AssetKind = "Animation"
            },
            Asset = new SpriteMetaFileAssetProperties
            {
                FrameSize = [frameWidth, frameHeight],
                Atlas = atlas,
                Offset = new SpriteMetaFileAssetOffset
                {
                    Horizontal = offsetH,
                    Vertical = offsetV
                }
            },
        };

        if (frameCount > 1)
        {
            animation.Asset.FrameCount = frameCount;
            animation.Asset.Duration = [duration];
        }
        
        return TomlSerializer.Serialize(animation);
    }
    
    private static string ShapeMeta(int w, int h, int ox, int oy)
    {
        var shape = new ShapeMeta
        {
            Meta = new MetaProperties
            {
                Id = IDManager.GenerateUniqueId(),
                AssetKind = "Shape"
            },
            Asset = new ShapeMetaAsset
            {
                Kind = "box",
                Offset = [ox, oy],
                Dimensions = [w, h]
            }
        };
        
        return TomlSerializer.Serialize(shape);
    }
    
    private static (int width, int height) DetectSize(IMod mod, string outfitSprite, int frameCount)
    {
        using var stream = mod.ReadFileAsStream(outfitSprite);
        var info = Image.Identify(stream);
        return (Math.Max(1, info.Width / frameCount), info.Height);
    }
}