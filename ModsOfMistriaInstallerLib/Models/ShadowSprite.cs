using Garethp.ModsOfMistriaInstallerLib.Generator;
using Garethp.ModsOfMistriaInstallerLib.Lang;
using Garethp.ModsOfMistriaInstallerLib.ModTypes;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Garethp.ModsOfMistriaInstallerLib.Models;

[JsonObject(NamingStrategyType = typeof(SnakeCaseNamingStrategy))]
public class ShadowSprite
{
    public string Name;

    public string Location;

    public string RegularSpriteName;

    public bool IsAnimated;
    
    public int? OriginX;

    public int? OriginY;

    public int? MarginLeft;

    public int? MarginRight;

    public int? MarginTop;

    public int? MarginBottom;

    [JsonProperty("sprite")]
    public string LegacySprite
    {
        set => Location = value;
    }
    
    public Validation Validate(Validation validation, IMod mod, string file, string id)
    {
        if (string.IsNullOrEmpty(RegularSpriteName))
        {
            validation.AddError(mod, file, string.Format(Resources.CoreErrorShadowHasNoSprite, id));
        }
        
        if (string.IsNullOrEmpty(Location))
        {
            validation.AddError(mod, file, string.Format(Resources.CoreErrorShadowHasNoLocation, id));
        }

        if (IsAnimated && ValidationTools.CheckSpriteDirectoryExists(mod, $"Shadow {id}", Location) is
                { } spriteDirectoryError)
        {
            validation.AddError(mod, file, spriteDirectoryError);
        }

        if (!IsAnimated && ValidationTools.CheckSpriteFileExists(mod, $"Shadow {id}", Location) is { } spriteFileError)
        {
            validation.AddError(mod, file, spriteFileError);
        }

        return validation;
    }
}