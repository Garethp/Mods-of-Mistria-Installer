using Tomlyn.Serialization;

namespace Garethp.ModsOfMistriaInstallerLib.Models.MOMI;

public class WearableFile
{
    public string Id;

    [TomlPropertyName("name")] 
    public string Name;
    
    [TomlPropertyName("description")]
    public string Description;

    [TomlPropertyName("ui_slut")]
    public string UiSlot;

    [TomlPropertyName("default_unlocked")]
    public string DefaultUnlocked;

    [TomlPropertyName("ui_sub_category")]
    public string UiSubCategory;

    [TomlPropertyName("lut_file")]
    public string LutFile;

    [TomlPropertyName("ui_file")]
    public string UiFile;

    [TomlPropertyName("outline_file")]
    public string OutlineFile;

    [TomlPropertyName("animation_files")]
    public Dictionary<string, string> AnimationFiles;

    [TomlPropertyName("price_override")]
    public int PriceOverride;

    [TomlPropertyName("frame_width")]
    public int? FrameWidth;
}