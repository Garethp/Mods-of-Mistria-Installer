using UndertaleModLib.Models;

namespace Garethp.ModsOfMistriaInstaller.Installer.UMT;

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

    public Dictionary<string, UndertaleTexturePageItem> PageItems = [];
}