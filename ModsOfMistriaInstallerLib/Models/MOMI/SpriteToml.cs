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

    [TomlPropertyName("sprite_type")]
    public string Type;

    [TomlPropertyName("install_location")]
    public string? InstallLocation
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

    [TomlPropertyName("duration")]
    public double Duration;

    [TomlPropertyName("offset")]
    public List<int> Offset;

    [TomlPropertyName("poly_kind")]
    public string PolyKind;

    [TomlPropertyName("poly_offset")]
    public List<int> PolyOffset;
}