using Garethp.ModsOfMistriaInstallerLib.Models.SDK;
using Tomlyn.Serialization;

namespace Garethp.ModsOfMistriaInstallerLib.Models.MOMI;

public class SpriteToml
{
    public string Id
    {
        get;
        set
        {
            if (value.StartsWith("spr_"))
            {
                value = value[4..];
            }
            
            field = value;
        }
    }

    [TomlPropertyName("location")]
    public string Location;

    [TomlPropertyName("fom_folder")]
    public string? FoMFolder
    {
        get;
        set
        {
            if (value is null)
            {
                field = null;
                return;
            }
            
            value = value.Replace(@"\", "/");

            if (value.StartsWith("/"))
            {
                value = value[1..];
            }

            if (value.StartsWith("assets/"))
            {
                value = value[7..];
            }

            if (value.StartsWith("animations/"))
            {
                value = value[11..];
            }
            
            field = value;
        }
    }

    [TomlPropertyName("frame_count")]
    public int FrameCount;

    [TomlPropertyName("create_poly_file")]
    public bool CreatePoly;

    [TomlPropertyName("meta")]
    public SpriteMetaFileAssetProperties? MetaProperties;
    
    [TomlPropertyName("poly")]
    public ShapeMetaAsset? PolyProperties;
}