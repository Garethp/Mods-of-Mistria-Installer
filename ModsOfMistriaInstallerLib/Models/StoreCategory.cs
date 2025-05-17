using Garethp.ModsOfMistriaInstallerLib.Generator;
using Garethp.ModsOfMistriaInstallerLib.Lang;
using Garethp.ModsOfMistriaInstallerLib.ModTypes;
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

    public Validation Validate(Validation validation, IMod mod, string file)
    {
        if (string.IsNullOrWhiteSpace(Store))
        {
            validation.AddError(mod, file, Resources.CoreErrorStoreCategoryHasNoStore);
        }
        
        if (string.IsNullOrWhiteSpace(IconName))
        {
            validation.AddError(mod, file, Resources.CoreErrorStoreCategoryNoName);
        }
        
        if (ValidationTools.CheckSpriteFileExists(mod, $"Category's sprite", Sprite) is { } spriteError)
        {
            validation.AddError(mod, file, spriteError);
        }
        
        return validation;
    }
}