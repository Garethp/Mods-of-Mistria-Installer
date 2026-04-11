using Garethp.ModsOfMistriaInstallerLib.Generator;
using Garethp.ModsOfMistriaInstallerLib.ModTypes;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Garethp.ModsOfMistriaInstallerLib.Models;

[JsonObject(NamingStrategyType = typeof(SnakeCaseNamingStrategy))]
public class Sprite
{
    public string Name;

    public string Location;

    public string? OutlineLocation;
    
    public bool IsAnimated;

    public uint? BoundingBoxMode;
    
    public bool IsPlayerSprite;

    public bool IsUiSprite;

    public int? OriginX;

    public int? OriginY;

    public int? MarginLeft;

    public int? MarginRight;

    public int? MarginTop;

    public int? MarginBottom;

    [JsonProperty("OutlineLocation")]
    private string? OutlineLocationLegacy
    {
        set => OutlineLocation = value;
    }
    
    [JsonProperty("IsAnimated")]
    private bool IsAnimatedLegacy
    {
        set => IsAnimated = value;
    }

    [JsonProperty("BoundingBoxMode")]
    private uint? BoundingBoxModeLegacy
    {
        set => BoundingBoxMode = value;
    }

    [JsonProperty("IsPlayerSprite")]
    private bool IsPlayerSpriteLegacy
    {
        set => IsPlayerSprite = value;
    }

    [JsonProperty("IsUiSprite")]
    private bool IsUiSpriteLegacy
    {
        set => IsUiSprite = value;
    }

    [JsonProperty("OriginX")]
    private int? OriginXLegacy
    {
        set => OriginX = value;
    }
    
    [JsonProperty("OriginY")]
    private int? OriginYLegacy
    {
        set => OriginY = value;
    }

    [JsonProperty("MarginLeft")]
    private int? MarginLeftLegacy
    {
        set => MarginLeft = value;
    }

    [JsonProperty("MarginRight")]
    private int? MarginRightLegacy
    {
        set => MarginRight = value;
    }

    [JsonProperty("MarginTop")]
    private int? MarginTopLegacy
    {
        set => MarginTop = value;
    }

    [JsonProperty("MarginBottom")]
    private int? MarginBottomLegacy
    {
        set => MarginBottom = value;
    }
    
    public Validation Validate(Validation validation, IMod mod, string file)
    {
        if (IsAnimated && ValidationTools.CheckSpriteDirectoryExists(mod, $"Sprite {Name}'s location", Location) is { } singleSpriteError)
        {
            validation.AddError(mod, file, singleSpriteError);
        }
        
        if (!IsAnimated && ValidationTools.CheckSpriteFileExists(mod, $"Sprite {Name}'s location", Location) is { } animatedSpriteError)
        {
            validation.AddError(mod, file, animatedSpriteError);
        }
        
        return validation;
    }
}