using Garethp.ModsOfMistriaInstallerLib.Generator;
using Garethp.ModsOfMistriaInstallerLib.Lang;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Garethp.ModsOfMistriaInstallerLib.Models;

[JsonObject(NamingStrategyType = typeof(SnakeCaseNamingStrategy))]
public class StoreCategory
{
    public string IconName;

    public string Store;

    public string Sprite;

    public int? RandomSelections;

    public Validation Validate(Validation validation, Mod mod, string file)
    {
        if (string.IsNullOrWhiteSpace(IconName))
        {
            validation.AddError(mod, file, Resources.ErrorStoreCategoryNoName);
        }
        
        if (ValidationTools.CheckSpriteFileExists(mod, $"Category's sprite", Sprite) is { } spriteError)
        {
            validation.AddError(mod, file, spriteError);
        }
        
        return validation;
    }
}