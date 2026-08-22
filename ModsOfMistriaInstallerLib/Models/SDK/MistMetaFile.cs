using Tomlyn.Model;
using Tomlyn.Serialization;

namespace Garethp.ModsOfMistriaInstallerLib.Models.SDK;

public class MistMetaFile
{
    [TomlPropertyName("meta_properties")] public MetaProperties Properties { get; set; } = new ("Mist");

    [TomlPropertyName("asset_properties")] public TomlTable AssetProperties { get; set; } = new ();
}