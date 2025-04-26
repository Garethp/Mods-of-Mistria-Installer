using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Garethp.ModsOfMistriaInstallerLib.Models;

[JsonObject(NamingStrategyType = typeof(SnakeCaseNamingStrategy))]
public class NewItem
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
    
    public object Data;
}