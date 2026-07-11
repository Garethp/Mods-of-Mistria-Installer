using Tomlyn.Serialization;

namespace Garethp.ModsOfMistriaInstallerLib.Models;

public class SpriteMetaFile
{
    [TomlPropertyName("meta_properties")] public SpriteMetaFileProperties? Meta { get; set; }

    [TomlPropertyName("asset_properties")] public SpriteMetaFileAssetProperties? Asset { get; set; }

    public void Merge(SpriteMetaFile? newProperties)
    {
        if (newProperties is null) return;
        
        if (Meta is null)
            Meta = newProperties.Meta;
        else
            Meta.Merge(newProperties.Meta);

        if (Asset is null)
            Asset = newProperties.Asset;
        else
            Asset.Merge(newProperties.Asset);
    }
}

public class SpriteMetaFileProperties
{
    [TomlPropertyName("id")] public string? Id { get; set; }
    
    [TomlPropertyName("replace_id")] public string? ReplaceId { get; set; }

    [TomlPropertyName("asset_kind")] public string? AssetKind { get; set; }

    public void Merge(SpriteMetaFileProperties? newProperties)
    {
        if (newProperties is null) return;
        
        if (!string.IsNullOrEmpty(newProperties.Id))
            Id = newProperties.Id;

        if (!string.IsNullOrEmpty(newProperties.ReplaceId))
        {
            Id = newProperties.ReplaceId;
            ReplaceId = newProperties.ReplaceId;
        }

        if (!string.IsNullOrEmpty(newProperties.AssetKind))
            AssetKind = newProperties.AssetKind;
    }
}

public class SpriteMetaFileAssetProperties
{
    [TomlPropertyName("atlas")] public string? Atlas { get; set; }

    public int FrameWidth
    {
        get => FrameSize[0];
        set => FrameSize[0] = value;
    }

    public int FrameHeight
    {
        get => FrameSize[1];
        set => FrameSize[1] = value;
    }

    [TomlPropertyName("frame_size")] public List<int> FrameSize { get; set; } = [0, 0];

    [TomlPropertyName("dimensions")] public List<int>? Dimensions { get; set; }

    [TomlPropertyName("frame_len")] public int FrameCount { get; set; } = 0;
    
    [TomlPropertyName("duration")] public List<float>? Duration { get; set; }

    [TomlPropertyName("offset")] public SpriteMetaFileAssetOffset? Offset { get; set; }

    public void Merge(SpriteMetaFileAssetProperties? newProperties)
    {
        if (newProperties is null) return;
        
        if (!string.IsNullOrEmpty(newProperties.Atlas)) Atlas = newProperties.Atlas;
        if (newProperties.FrameCount > 1) FrameCount = newProperties.FrameCount;
        if (newProperties.FrameWidth > 0) FrameWidth = newProperties.FrameWidth;
        if (newProperties.FrameHeight > 0) FrameHeight = newProperties.FrameHeight;
        if (newProperties.Duration is not null) Duration = newProperties.Duration;
        if (newProperties.Dimensions is not null) Dimensions = newProperties.Dimensions;

        if (Offset is null)
            Offset = newProperties.Offset;
        else
            Offset.Merge(newProperties.Offset);
    }
}

public class SpriteMetaFileAssetOffset
{
    [TomlPropertyName("horizontal")] public object? Horizontal { get; set; }

    [TomlPropertyName("vertical")] public object? Vertical { get; set; }

    public void Merge(SpriteMetaFileAssetOffset? newProperties)
    {
        if (newProperties is null) return;
        
        if (newProperties.Horizontal is not null) Horizontal = newProperties.Horizontal;
        if (newProperties.Vertical is not null) Vertical = newProperties.Vertical;
    }
}