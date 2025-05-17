using Garethp.ModsOfMistriaInstallerLib.Generator;
using Garethp.ModsOfMistriaInstallerLib.Lang;
using Garethp.ModsOfMistriaInstallerLib.ModTypes;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Garethp.ModsOfMistriaInstallerLib.Models;

[JsonObject(NamingStrategyType = typeof(SnakeCaseNamingStrategy))]
public class NewItem
{
    public string Name;

    public bool? OverwritesOtherMod;
    
    public object Data;
    
    public bool ShouldSerializeOverwritesOtherMod() => false;

    public Validation Validate(Validation validation, IMod mod, string file, string id)
    {
        if (OverwritesOtherMod is null)
        {
            validation.AddError(mod, file, string.Format(Resources.CoreErrorNewItemHasNoOverwritesOtherMod, Name));
        }
        
        return validation;
    }
}