using Garethp.ModsOfMistriaInstallerLib.Generator;
using Garethp.ModsOfMistriaInstallerLib.Lang;
using Garethp.ModsOfMistriaInstallerLib.ModTypes;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Garethp.ModsOfMistriaInstallerLib.Models;

[JsonObject(NamingStrategyType = typeof(SnakeCaseNamingStrategy))]
public class NewObject
{
    public string Name;

    public bool? OverwritesOtherMod;

    public string Category;

    public object Data;

    public static List<string> ValidCategories =
    [
        "breakable",
        "building",
        "bush",
        "crop",
        "dig_site",
        "furniture",
        "grass",
        "rock",
        "stump",
        "tree"
    ];

    public bool ShouldSerializeOverwritesOtherMod() => false;
    
    public Validation Validate(Validation validation, IMod mod, string file, string id)
    {
        if (string.IsNullOrEmpty(Name))
        {
            validation.AddError(mod, file, Resources.CoreErrorNewObjectNoName);
            return validation;
        }
        
        if (OverwritesOtherMod is null)
        {
            validation.AddError(mod, file, string.Format(Resources.CoreErrorNewObjectHasNoOverwritesOtherMod, Name));
        }
        
        if (string.IsNullOrEmpty(Category))
        {
            validation.AddError(mod, file, string.Format(Resources.CoreErrorNewObjectNoCategory, Name));
        }  else if (!ValidCategories.Contains(Category)) {
            validation.AddError(mod, file, string.Format(Resources.CoreErrorNewObjectInvalidCategory, Name, Category));
        }
        
        if (Data.ToString()?.Replace(" ","") == "{}")
        {
            validation.AddError(mod, file, string.Format(Resources.CoreErrorNewObjectNoData, Name));
        }
        
        return validation;
    }
}