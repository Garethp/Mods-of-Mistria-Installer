using Garethp.ModsOfMistriaInstallerLib.Collector;
using Garethp.ModsOfMistriaInstallerLib.ModTypes;
using Tomlyn;
using Tomlyn.Model;

namespace Garethp.ModsOfMistriaInstallerLib;

public class GeneratedInformation
{
    public List<GeneratedTomlItem> Toml = [];
    
    public List<AnimationGroup> AnimationGroups = [];
    
    public void Merge(GeneratedInformation information)
    {
        Toml.AddRange(information.Toml);
        AnimationGroups.AddRange(information.AnimationGroups);
    }
}

public class GeneratedTomlItem
{
    public string FilePath;
    
    public string? ReadFilePath;

    public string? Contents;

    public TomlTable ReadToml(IMod mod)
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

    public string ReadString(IMod mod)
    {
        if (!string.IsNullOrEmpty(Contents))
        {
            return Contents;
        }

        if (!string.IsNullOrEmpty(ReadFilePath))
        {
            return mod.ReadFile(ReadFilePath);
        }

        // TODO: Should this throw an exception?
        return "";
    }
}