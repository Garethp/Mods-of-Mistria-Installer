using Tomlyn;
using Tomlyn.Serialization;

namespace Garethp.ModsOfMistriaInstallerLib.Models;

public class AtlasMetaFile
{
    [TomlPropertyName("meta_properties")] public AtlasMetaFileMetaProperties Meta { get; set; } = new();

    [TomlPropertyName("asset_properties")] public AtlasAssetProperties Asset { get; set; } = new();
}

public class AtlasMetaFileMetaProperties
{
    [TomlPropertyName("id")] public string Id { get; set; }
    
    [TomlPropertyName("asset_kind")] public string AssetKind { get; set; }
}

public class AtlasAssetProperties
{
    [TomlPropertyName("dimensions")] public List<int> Dimensions { get; set; }
    
    [TomlPropertyName("filter_kind")] public string? Filter { get; set; }
    
    [TomlPropertyName("mipmap_filter_kind")] public string? MipmapFilter { get; set; }
    
    [TomlPropertyName("texture_wrap")] public string? TextureWrap { get; set; }
    
    [TomlPropertyName("srgb")] public bool? Srgb { get; set; }
    
    [TomlTableArrayStyle(TomlTableArrayStyle.Headers)]
    [TomlPropertyName("animations")] public List<AtlasAnimation> Animations { get; set; } = new();
}

public class AtlasAnimation
{
    [TomlPropertyName("texture_ids")] public List<string> TextureIds { get; set; }
    
    [TomlPropertyName("placement")] public List<int> Placement { get; set; }
}