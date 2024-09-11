using UndertaleModLib.Models;

namespace Garethp.ModsOfMistriaInstaller.Installer.UMT;

public class SpriteData
{
    public string Name;

    public string Location;
    
    public bool HasFrames;

    public uint BoundingBoxMode;
    
    public bool DeleteCollisionMask;
    
    public bool SpecialType;
    
    public uint SpecialTypeVersion;
    
    public int SpecialPlaybackSpeed;
    
    public bool IsPlayerSprite;

    public bool IsUiSprite;

    public Dictionary<string, UndertaleTexturePageItem> PageItems = [];
}