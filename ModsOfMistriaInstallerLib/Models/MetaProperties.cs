using Tomlyn.Serialization;

namespace Garethp.ModsOfMistriaInstallerLib.Models;

public class MetaProperties
{
    [TomlPropertyName("id")] public string Id { get; set; }
    
    [TomlPropertyName("asset_kind")] public string AssetKind { get; set; }
    
    [TomlPropertyName("required_assets")] public List<string>? RequiredAssets { get; set; }
    
    public virtual void Merge(MetaProperties? newProperties)
    {
        if (newProperties is null) return;
        
        if (!string.IsNullOrEmpty(newProperties.Id))
            Id = newProperties.Id;

        if (!string.IsNullOrEmpty(newProperties.AssetKind))
            AssetKind = newProperties.AssetKind;

        if (newProperties.RequiredAssets is not null && newProperties.RequiredAssets.Count > 0)
        {
            RequiredAssets = newProperties.RequiredAssets;
        }
    }
}