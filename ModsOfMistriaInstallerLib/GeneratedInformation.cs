using Garethp.ModsOfMistriaInstallerLib.Models;

namespace Garethp.ModsOfMistriaInstallerLib;

public class GeneratedInformation
{
    public Dictionary<string, object?> Toml = new(StringComparer.OrdinalIgnoreCase);

    public void Merge(GeneratedInformation information)
    {
        foreach (var key in information.Toml.Keys)
        {
            if (!Toml.ContainsKey(key)) Toml.Add(key, Toml[key]);
        }
    }
}