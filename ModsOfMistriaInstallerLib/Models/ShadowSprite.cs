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

    public string Sprite;

    public string RegularSpriteName;

    public bool IsAnimated;

    public Validation Validate(Validation validation, IMod mod, string file, string id)
    {
        if (string.IsNullOrEmpty(RegularSpriteName))
        {
            validation.AddError(mod, file, string.Format(Resources.CoreErrorShadowHasNoSprite, id));
        }
        
        if (string.IsNullOrEmpty(Sprite))
        {
            validation.AddError(mod, file, string.Format(Resources.CoreErrorShadowHasNoLocation, id));
        }

        if (IsAnimated && ValidationTools.CheckSpriteDirectoryExists(mod, $"Shadow {id}", Sprite) is
                { } spriteDirectoryError)
        {
            validation.AddError(mod, file, spriteDirectoryError);
        }

        if (!IsAnimated && ValidationTools.CheckSpriteFileExists(mod, $"Shadow {id}", Sprite) is { } spriteFileError)
        {
            validation.AddError(mod, file, spriteFileError);
        }

        return validation;
    }
}