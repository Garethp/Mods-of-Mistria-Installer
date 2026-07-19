using Tomlyn.Serialization;

namespace Garethp.ModsOfMistriaInstallerLib.Models;

public class ShapeMeta
{
    [TomlPropertyName("meta_properties")] public MetaProperties Meta { get; set; }
    
    [TomlPropertyName("asset_properties")] public ShapeMetaAsset Asset { get; set; }
}

public class ShapeMetaAsset
{
    [TomlPropertyName("kind")] public string Kind { get; set; }
    
    [TomlPropertyName("offset")] public List<int>? Offset { get; set; }
    [TomlPropertyName("dimensions")] public List<int>? Dimensions { get; set; }
    [TomlPropertyName("radius")] public int? Radius { get; set; }
}