using Garethp.ModsOfMistriaInstallerLib.Models;
using Garethp.ModsOfMistriaInstallerLib.ModTypes;
using Tomlyn;
using Tomlyn.Model;

namespace Garethp.ModsOfMistriaInstallerLib;

public class GeneratedInformation
{
    public List<GeneratedTomlItem> Toml = [];

    public void Merge(GeneratedInformation information)
    {
        Toml.AddRange(information.Toml);
    }
}

public class GeneratedTomlItem
{
    public string FilePath;
    
    public string? ReadFilePath;

    public string? Contents;

    public TomlTable Read(IMod mod)
    {
        if (!string.IsNullOrEmpty(Contents))
        {
            return TomlSerializer.Deserialize<TomlTable>(Contents)!;
        }

        if (!string.IsNullOrEmpty(ReadFilePath))
        {
            return TomlSerializer.Deserialize<TomlTable>(mod.ReadFile(ReadFilePath))!;
        }

        // TODO: Should this throw an exception?
        return new TomlTable();
    }
}