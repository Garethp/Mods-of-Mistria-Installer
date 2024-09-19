using Garethp.ModsOfMistriaInstallerLib.Generator;
using Newtonsoft.Json;
using UndertaleModLib.Models;

namespace Garethp.ModsOfMistriaInstallerLib.Installer.UMT;

[JsonObject]
public class SpriteData
{
    public string Name;

    public string Location;

    public string BaseLocation;
    
    public bool IsAnimated = false;

    public uint? BoundingBoxMode;
    
    public bool DeleteCollisionMask = true;
    
    public bool SpecialType = true;
    
    public uint SpecialTypeVersion = 3;
    
    public int SpecialPlaybackSpeed = 40;

    public bool IsPlayerSprite = false;

    public bool IsUiSprite = false;

    public int? OriginX;

    public int? OriginY;

    public int? MarginLeft;

    public int? MarginRight;

    public int? MarginTop;

    public int? MarginBottom;

    public Dictionary<string, UndertaleTexturePageItem> PageItems = [];

    public Validation Validate(Validation validation, Mod mod, string file)
    {
        if (IsAnimated && ValidationTools.CheckSpriteDirectoryExists(mod, Name, Location) is { } singleSpriteError)
        {
            validation.AddError(mod, file, singleSpriteError);
        }
        
        if (!IsAnimated && ValidationTools.CheckSpriteFileExists(mod, Name, Location) is { } animatedSpriteError)
        {
            validation.AddError(mod, file, animatedSpriteError);
        }
        
        return validation;
    }
}

public class TilesetData
{
    public string Name;

    public string Location;

    public string BaseLocation;
}