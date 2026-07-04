using Newtonsoft.Json.Linq;
using Tomlyn.Model;

namespace Garethp.ModsOfMistriaInstallerLib.ModTypes;

public class ModManifest
{
    public readonly string Name;
    public readonly string Author;
    public readonly string Version;
    public readonly string MinInstallerVersion;
    public readonly string ManifestVersion;
    public List<ModRequirement> Requirements;
    public readonly string? DownloadUrl;
    public readonly string? UpdateUrl;

    public ModManifest(
        string name, 
        string author, 
        string version, 
        string minInstallerVersion, 
        string manifestVersion,
        List<ModRequirement> requirements, 
        string? downloadUrl, 
        string? updateUrl
    ) {
        Name = name;
        Author = author;
        Version = version;
        MinInstallerVersion = minInstallerVersion;
        ManifestVersion = manifestVersion;
        Requirements = requirements;
        DownloadUrl = downloadUrl;
        UpdateUrl = updateUrl;
    }

    public static ModManifest FromJson(JObject json)
    {
        return new ModManifest(
            json["name"]?.ToString() ?? "",
            json["author"]?.ToString() ?? "",
            json["version"]?.ToString() ?? "",
            json["minInstallerVersion"]?.ToString() ?? "0.1",
            json["manifestVersion"]?.ToString() ?? "1",
            (json["requirements"] as JArray ?? [])
            .Select(r => new ModRequirement(
                r["name"]?.ToString() ?? "",
                r["author"]?.ToString() ?? "",
                r["download_url"]?.ToString()))
            .Where(r => !string.IsNullOrEmpty(r.Name) && !string.IsNullOrEmpty(r.Author))
            .ToList(),
            json["download_url"]?.ToString(),
            json["update_url"]?.ToString()
        );
    }

    public static ModManifest FromToml(TomlTable toml)
    {
        toml.TryGetValue("name", out var name);
        toml.TryGetValue("author", out var author);
        toml.TryGetValue("version", out var version);
        toml.TryGetValue("minInstallerVersion", out var minInstallerVersion);
        toml.TryGetValue("manifestVersion", out var manifestVersion);
        toml.TryGetValue("requirements", out var requirementsObject);
        toml.TryGetValue("download_url", out var downloadUrl);
        toml.TryGetValue("update_url", out var updateUrl);

        List<ModRequirement> requirements = [];
        IList<TomlTable> requirementsList = new List<TomlTable>();

        if (requirementsObject is IList<object?> requirementsListUnknown)
        {
            requirementsList = requirementsListUnknown.OfType<TomlTable>().ToList();
        } else if (requirementsObject is IList<TomlTable> requirementsListKnown)
        {
            requirementsList = requirementsListKnown;
        }
        
        requirements.AddRange(requirementsList.Select(requirement =>
        {
            requirement.TryGetValue("name", out var name);
            requirement.TryGetValue("author", out var author);
            requirement.TryGetValue("download_url", out var downloadUrl);
            
            return new ModRequirement(name?.ToString() ?? "", author?.ToString() ?? "", downloadUrl?.ToString());
        }));

        return new ModManifest(
            name?.ToString() ?? "",
            author?.ToString() ?? "",
            version?.ToString() ?? "",
            minInstallerVersion?.ToString() ?? "0.1",
            manifestVersion?.ToString() ?? "1",
            requirements,
            downloadUrl?.ToString(),
            updateUrl?.ToString()
        );
    }
}