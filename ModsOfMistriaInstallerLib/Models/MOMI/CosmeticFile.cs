using Garethp.ModsOfMistriaInstallerLib.Generator;
using Garethp.ModsOfMistriaInstallerLib.Lang;
using Garethp.ModsOfMistriaInstallerLib.ModTypes;
using Tomlyn.Serialization;

namespace Garethp.ModsOfMistriaInstallerLib.Models.MOMI;

public class CosmeticFile
{
    public static Dictionary<string, List<string>> ValidSlots = new()
    {
        { "back", ["capes", "backpacks", "back_gear_misc"] },
        { "facial_hair", ["facial_hair"] },
        { "top", ["dress", "robe", "top_misc", "suit", "long_sleeve", "sleeveless", "short_sleeve", "jacket"] },
        { "eyes", ["eyes" ] },
        { "face_gear", ["face_accessory", "ear_accessory", "glasses"] },
        { "hair", ["medium_hair", "short_hair", "long_hair"] },
        { "head_gear", ["crown", "head_gear_misc", "hair_accessory", "hat", "helmet"] },
        { "bottom", ["pants", "bottom_misc", "shorts", "skirt"] },
        { "feet", ["boots", "feet_misc", "sandals", "shoes"] }
    };
    
    private static Dictionary<string, int> PartsFrameCount = new()
    {
        { "hair_front", 49 },
        { "hair_mid", 49 },
        { "hair_back", 49 },
        { "head_gear", 18 },
        { "head_gear_back", 18 },
        { "eyes", 13 },
        { "face_gear", 20 },
        { "facial_hair", 22 },
        { "beard", 22 },
        { "torso", 14 },
        { "back_gear", 59 },
        { "sleeve_left", 49 },
        { "sleeve_right", 57 },
        { "waist", 48 },
        { "legs_top", 48 },
        { "legs", 48 },
        { "feet", 41 },
    };
    
    public string Id;

    [TomlPropertyName("name")] 
    public string Name { get; set; }

    [TomlPropertyName("ui_slot")]
    public string UiSlot { get; set; }

    [TomlPropertyName("ui_sub_category")]
    public string UiSubCategory { get; set; }
    
    /**
     * Either this or lut_sprite must be defined
     */
    [TomlPropertyName("lut")]
    public string? LutFile { get; set; }
    
    /**
     * Either this or lut_file must be defined
     */
    [TomlPropertyName("lut_sprite")]
    public string? LutSprite { get; set; }

    [TomlPropertyName("ui_sprites")]
    public CosmeticUiSprites UiSprites { get; set; } = new();

    [TomlPropertyName("cosmetic_sprites")]
    public Dictionary<string, string> CosmeticSprites { get; set; } = new();

    [TomlPropertyName("price_override")]
    public int? PriceOverride { get; set; }
    
    [TomlPropertyName("default_unlocked")]
    public bool? DefaultUnlocked { get; set; }

    public int GetPartFrameCount(string part)
    {
        if (!PartsFrameCount.ContainsKey(part))
        {
            throw new Exception(string.Format(Resources.CoreErrorCosmeticCosmeticSpritesWrong, Id, part,
                string.Join(", ", PartsFrameCount.Keys)));
        }
        
        return PartsFrameCount[part];
    }
    
    public Validation Validate(Validation validation, IMod mod, string file, string id)
    {
        if (string.IsNullOrWhiteSpace(Name))
        {
            validation.AddError(mod, file, Resources.CoreErrorCosmeticNoName);
        }

        if (string.IsNullOrWhiteSpace(UiSlot))
        {
            validation.AddError(mod, file, string.Format(Resources.CoreErrorCosmeticNoUiSlot, id));
        }
        else if (!ValidSlots.ContainsKey(UiSlot))
        {
            validation.AddError(mod, file, string.Format(Resources.CoreErrorCosmeticUiSlotWrong, id, string.Join(", ", ValidSlots.Keys)));
        }

        if (string.IsNullOrWhiteSpace(UiSubCategory))
        {
            validation.AddError(mod, file, string.Format(Resources.CoreErrorCosmeticNoSubCategory, id));
        } else if (!string.IsNullOrEmpty(UiSlot) && ValidSlots.ContainsKey(UiSlot) && !ValidSlots[UiSlot].Contains(UiSubCategory))
        {
            validation.AddError(mod, file, string.Format(Resources.CoreCosmeticUiSubCategoryWrong, id, string.Join(", ", ValidSlots[UiSlot])));
        }

        if (string.IsNullOrWhiteSpace(LutFile) && string.IsNullOrWhiteSpace(LutSprite))
        {
            validation.AddError(mod, file, string.Format(Resources.CoreErrorCosmeticNoLut, id));
        } else if (!string.IsNullOrEmpty(LutFile) &&
                   ValidationTools.CheckSpriteFileExists(mod, $"Cosmetic {id}'s lut", LutFile) is { } lutError)
        {
            validation.AddError(mod, file, lutError);
        }
        
        if (CosmeticSprites.Count == 0)
        {
            validation.AddError(mod, file, string.Format(Resources.CoreErrorCosmeticNoCosmeticSprites, id));
        }

        foreach (var bodyPart in CosmeticSprites.Keys)
        {
            if (!PartsFrameCount.ContainsKey(bodyPart))
            {
                validation.AddError(mod, file, string.Format(Resources.CoreErrorCosmeticCosmeticSpritesWrong, id, bodyPart, string.Join(", ", PartsFrameCount.Keys)));
            }
            
            if (ValidationTools.CheckSpriteFileExists(mod, $"Cosmetic {id}'s cosmetic_sprite file {bodyPart}",
                    CosmeticSprites[bodyPart]) is { } animationError)
            {
                validation.AddError(mod, file, animationError);
            }
        }
        
        validation = UiSprites.Validate(validation, mod, file, id);

        if (PriceOverride is not null && PriceOverride < 0)
        {
            validation.AddError(mod, file, string.Format(Resources.CoreErrorCosmeticPriceOverrideNegative, id));
        }
        
        return validation;
    }
}

public class CosmeticUiSprites
{
    /**
     * Either the UiFile should be defined OR the Below files should be defined
     */
    [TomlPropertyName("ui")]
    public string? UiFile { get; set; }
    
    [TomlPropertyName("outline")]
    public string? OutlineFile { get; set; }
    
    [TomlPropertyName("asset")]
    public string? AssetFile { get; set; }
    
    [TomlPropertyName("body")]
    public string? BodyFile { get; set; }

    [TomlPropertyName("merged")]
    public string? MergedFile { get; set; }
    
    [TomlPropertyName("merged_outline")]
    public string? MergedOutlineFile { get; set; }

    public Validation Validate(Validation validation, IMod mod, string file, string id)
    {
        if (string.IsNullOrWhiteSpace(UiFile) && string.IsNullOrWhiteSpace(AssetFile))
        {
            validation.AddError(mod, file, string.Format(Resources.CoreErrorCosmeticNoUiSprites, id));
            return validation;
        }
        
        if (!string.IsNullOrWhiteSpace(UiFile) && !string.IsNullOrWhiteSpace(AssetFile))
        {
            validation.AddError(mod, file, string.Format(Resources.CoreErrorCosmeticUiSpritesAllDefined, id));
            return validation;
        }

        if (!string.IsNullOrEmpty(UiFile))
        {
            if (ValidationTools.CheckSpriteFileExists(mod, $"Cosmetic {id}'s ui_sprites.ui file", UiFile) is
                { } uiFileError)
            {
                validation.AddError(mod, file, uiFileError);
            }

            if (string.IsNullOrEmpty(OutlineFile))
            {
                validation.AddError(mod, file, string.Format(Resources.CoreErrorCosmeticSpriteNoOutline, id));
            }
            else if (ValidationTools.CheckSpriteFileExists(mod, $"Cosmetic {id}'s ui_sprites.outline file", OutlineFile) is { } outlineFileError)
            {
                validation.AddError(mod, file, outlineFileError);
            }
        }

        if (!string.IsNullOrEmpty(AssetFile))
        {
            if (ValidationTools.CheckSpriteFileExists(mod, $"Cosmetic {id}'s ui_sprites.asset file", AssetFile) is
                { } assetFileError)
            {
                validation.AddError(mod, file, assetFileError);
            }

            if (string.IsNullOrEmpty(BodyFile))
            {
                validation.AddError(mod, file, string.Format(Resources.CoreErrorCosmeticUiSpriteNoBody, id));
            } else if (ValidationTools.CheckSpriteFileExists(mod, $"Cosmetic {id}'s ui_sprites.body file", BodyFile) is
                       { } bodyFileError)
            {
                validation.AddError(mod, file, bodyFileError);
            }
            
            if (string.IsNullOrEmpty(MergedFile))
            {
                validation.AddError(mod, file, string.Format(Resources.CoreErrorCosmeticUiSpritesNoMerged, id));
            } else if (ValidationTools.CheckSpriteFileExists(mod, $"Cosmetic {id}'s ui_sprites.merged file", MergedFile) is
                       { } mergedFileError)
            {
                validation.AddError(mod, file, mergedFileError);
            }
            
            if (string.IsNullOrEmpty(MergedOutlineFile))
            {
                validation.AddError(mod, file, string.Format(Resources.CoreErrorCosmeticUiSpritesNoMergedOutline, id));
            } else if (ValidationTools.CheckSpriteFileExists(mod, $"Cosmetic {id}'s ui_sprites.merged_outline file", MergedOutlineFile) is
                       { } mergedOutlineFileError)
            {
                validation.AddError(mod, file, mergedOutlineFileError);
            }
        }

        return validation;
    }
}