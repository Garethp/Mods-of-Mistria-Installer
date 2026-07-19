using Tomlyn.Serialization;

namespace Garethp.ModsOfMistriaInstallerLib.Models;

public class OutfitFile
{
    public string Id { get; set; } = "";
    [TomlPropertyName("name")] public string Name { get; init; } = "";
    [TomlPropertyName("ui_slot")] public string UiSlot { get; set; } = "";
    [TomlPropertyName("ui_sub_category")] public string UiSubCategory { get; set; } = "";
    [TomlPropertyName("default_unlocked")] public bool DefaultUnlocked { get; set; } = true;

    [TomlPropertyName("outfit")] public string? OutfitSprite { get; set; }
    [TomlPropertyName("lut")] public string? LutSprite { get; init; }
    [TomlPropertyName("icon")] public string? IconSprite { get; init; }
    [TomlPropertyName("outline")] public string? OutlineSprite { get; init; }

    public int? FrameWidth { get; set; }
    public int? FrameHeight { get; set; }

    // Optional — written to player_assets.toml when present.
    [TomlPropertyName("price_override")] public int? PriceOverride { get; init; }

    // Resolved sprite names
    public string ResolvedOutfitSprite => OutfitSprite ?? $"spr_player_{Id}_{UiSlot}";
    public string ResolvedLutSprite => LutSprite ?? $"spr_player_{Id}_lut";
    public string ResolvedIconSprite => IconSprite ?? $"spr_ui_item_wearable_{Id}";
    public string ResolvedOutlineSprite => OutlineSprite ?? $"spr_ui_item_wearable_{Id}_outline";

    [TomlPropertyName("outfit_sprite")]
    public string? LegacyOutfitSprite
    {
        get;
        init => OutfitSprite ??= value;
    }

    [TomlPropertyName("lut_sprite")]
    public string? LegacyLutSprite
    {
        get;
        init => LutSprite ??= value;
    }
    
    [TomlPropertyName("icon_sprite")]
    public string? LegacyIconSprite
    {
        get;
        init => IconSprite ??= value;
    }
    
    [TomlPropertyName("outline_sprite")]
    public string? LegacyOutlineSprite
    {
        get;
        init => OutlineSprite ??= value;
    }

    [TomlPropertyName("frame_size")]
    public List<int> FrameSize
    {
        get;
        set
        {
            if (value.Count == 2)
            {
                field = value;
            }

            FrameWidth = value[0];
            FrameHeight = value[1];
        }
    }
}