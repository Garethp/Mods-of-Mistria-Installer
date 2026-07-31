using Tomlyn.Serialization;

namespace Garethp.ModsOfMistriaInstallerLib.Models.MOMI;

public class CosmeticFile
{
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
        { "sleeve_left", 49 },
        { "sleeve_right", 57 },
        { "waist", 48 },
        { "legs", 48 },
        { "feet", 41 },
    };
    
    public string Id;

    [TomlPropertyName("name")] 
    public string Name { get; set; }

    [TomlPropertyName("ui_slot")]
    public string UiSlot { get; set; }

    [TomlPropertyName("default_unlocked")]
    public bool? DefaultUnlocked { get; set; }

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

    [TomlPropertyName("frame_width")]
    public int? FrameWidth { get; set; }

    public int GetPartFrameCount(string part)
    {
        // @TODO: Throw an exception if an incorrect part is passed in

        return PartsFrameCount[part];
    }
}

public class CosmeticUiSprites
{
    /**
 * Either the UiFile should be defined OR the Below files should be defined
 */
    [TomlPropertyName("ui")]
    public string? UiFile { get; set; }
    
    [TomlPropertyName("asset")]
    public string? AssetFile { get; set; }
    
    [TomlPropertyName("body")]
    public string? BodyFile { get; set; }

    [TomlPropertyName("merged")]
    public string? MergedFile { get; set; }
    
    [TomlPropertyName("merged_outline")]
    public string? MergedOutlineFile { get; set; }
    
    [TomlPropertyName("outline")]
    public string? OutlineFile { get; set; }
}