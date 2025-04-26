using Garethp.ModsOfMistriaInstallerLib.Generator;
using Garethp.ModsOfMistriaInstallerLib.Lang;
using Garethp.ModsOfMistriaInstallerLib.ModTypes;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Garethp.ModsOfMistriaInstallerLib.Models;

[JsonObject(NamingStrategyType = typeof(SnakeCaseNamingStrategy))]
public class NewObject
{
    private string _name;
    
    public string Name
    {
        get => !DisablePrefix && !string.IsNullOrEmpty(Prefix) ? $"{Prefix.Replace(".", "_")}_{_name}" : _name;
        set => _name = value;
    }

    public string Prefix;

    public bool ShouldSerializePrefix() => false;
    
    public bool DisablePrefix = false;
    
    public bool ShouldSerializeDisablePrefix() => false;

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

    public Validation Validate(Validation validation, IMod mod, string file, string id)
    {
        if (string.IsNullOrEmpty(_name))
        {
            validation.AddError(mod, file, Resources.ErrorNewObjectNoName);
            return validation;
        }
        
        if (string.IsNullOrEmpty(Category))
        {
            validation.AddError(mod, file, string.Format(Resources.ErrorNewObjectNoCategory, _name));
        }  else if (!ValidCategories.Contains(Category)) {
            validation.AddError(mod, file, string.Format(Resources.ErrorNewObjectInvalidCategory, _name, Category));
        }
        
        if (Data.ToString()?.Replace(" ","") == "{}")
        {
            validation.AddError(mod, file, string.Format(Resources.ErrorNewObjectNoData, _name));
        }
        
        return validation;
    }
}