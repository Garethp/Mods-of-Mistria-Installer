using Garethp.ModsOfMistriaInstallerLib.Collector;
using Garethp.ModsOfMistriaInstallerLib.ModTypes;
using Newtonsoft.Json.Linq;
using Tomlyn;
using Tomlyn.Model;

namespace Garethp.ModsOfMistriaInstallerLib;

public class GeneratedInformation
{
    public List<GeneratedTomlItem> Toml = [];
    
    public List<JsonItem> Json = [];
    
    public Dictionary<string, AnimationGroup> AnimationGroups = [];
    
    public void Merge(GeneratedInformation information)
    {
        Toml.AddRange(information.Toml);
        Json.AddRange(information.Json);
        
        foreach (var key in information.AnimationGroups.Keys)
        {
            if (!AnimationGroups.ContainsKey(key)) AnimationGroups.Add(key, information.AnimationGroups[key]);
        }
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

    public static GeneratedTomlItem FromFileOrContents(IMod mod, string filePath, string contents)
    {
        if (mod.FileExists(filePath))
        {
            return new GeneratedTomlItem
            {
                FilePath = filePath,
                ReadFilePath = filePath
            };
        }

        return new GeneratedTomlItem
        {
            FilePath = filePath,
            Contents = contents
        };
    }
}

public class JsonItem
{
    public string FilePath;
    
    public string? ReadFilePath;

    public string? Contents;

    public JObject ReadJson(IMod mod)
    {
        if (!string.IsNullOrEmpty(Contents))
        {
            return JObject.Parse(Contents);
        }

        if (!string.IsNullOrEmpty(ReadFilePath))
        {
            return JObject.Parse(mod.ReadFile(ReadFilePath))!;
        }

        // TODO: Should this throw an exception?
        return new JObject();
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