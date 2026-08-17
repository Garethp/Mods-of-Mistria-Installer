using Newtonsoft.Json.Linq;
using Tomlyn.Model;

namespace Garethp.ModsOfMistriaInstallerLib.ModTypes;

public class ModManifest
{
    public readonly string Name;
    public readonly string Author;
    public readonly string Description;
    public readonly string Version;
    public readonly string MinInstallerVersion;
    public readonly string ManifestVersion;
    public List<ModRequirement> Requirements;
    public readonly string? DownloadUrl;
    public readonly string? UpdateUrl;

    // Optional AIM extensions. Unknown manifest fields remain harmless to
    // MOMI; these dictionaries let AIM use name_bg/description_bg-style
    // values while retaining the standard fields as the fallback.
    public readonly IReadOnlyDictionary<string, string> LocalizedNames;
    public readonly IReadOnlyDictionary<string, string> LocalizedDescriptions;

    // Hook names the mod cannot run without; the apply fails that mod closed
    // when the seam catalog lacks one
    public readonly List<string> RequiresHooks;

    // False when requires_hooks is not an array of strings
    public readonly bool RequiresHooksValid;

    public static string LocalizedValue(IReadOnlyDictionary<string, string> values, string fallback,
        string? languageCode)
    {
        if (string.IsNullOrWhiteSpace(languageCode)) return fallback;

        var language = languageCode.Trim().ToLowerInvariant();
        var candidates = new[] { language, language.Replace('-', '_'), language.Split('-', '_')[0] };
        foreach (var candidate in candidates)
        {
            if (values.TryGetValue(candidate, out var value) && !string.IsNullOrWhiteSpace(value))
                return value;
        }

        return fallback;
    }

    public ModManifest(
        string name,
        string author,
        string description,
        string version,
        string minInstallerVersion,
        string manifestVersion,
        List<ModRequirement> requirements,
        string? downloadUrl,
        string? updateUrl,
        List<string>? requiresHooks = null,
        bool requiresHooksValid = true,
        IReadOnlyDictionary<string, string>? localizedNames = null,
        IReadOnlyDictionary<string, string>? localizedDescriptions = null
    ) {
        Name = name;
        Author = author;
        Description = description;
        Version = version;
        MinInstallerVersion = minInstallerVersion;
        ManifestVersion = manifestVersion;
        Requirements = requirements;
        DownloadUrl = downloadUrl;
        UpdateUrl = updateUrl;
        LocalizedNames = localizedNames ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        LocalizedDescriptions = localizedDescriptions ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        RequiresHooks = requiresHooks ?? [];
        RequiresHooksValid = requiresHooksValid;
    }

    public static ModManifest FromJson(JObject json)
    {
        var localizedNames = ReadLocalizedFields(json, "name_");
        var localizedDescriptions = ReadLocalizedFields(json, "description_");
        var requiresHooksToken = json["requires_hooks"];
        var requiresHooksValid = requiresHooksToken is null
                                 || (requiresHooksToken is JArray hooks
                                     && hooks.All(h => h.Type == JTokenType.String));

        return new ModManifest(
            json["name"]?.ToString() ?? "",
            json["author"]?.ToString() ?? "",
            json["description"]?.ToString() ?? "",
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
            json["update_url"]?.ToString(),
            requiresHooksValid
                ? (requiresHooksToken as JArray ?? []).Select(h => h.ToString().Trim()).ToList()
                : [],
            requiresHooksValid,
            localizedNames,
            localizedDescriptions
        );
    }

    public static ModManifest FromToml(TomlTable toml)
    {
        var localizedNames = ReadLocalizedFields(toml, "name_");
        var localizedDescriptions = ReadLocalizedFields(toml, "description_");
        toml.TryGetValue("name", out var name);
        toml.TryGetValue("author", out var author);
        toml.TryGetValue("description", out var description);
        toml.TryGetValue("version", out var version);
        toml.TryGetValue("minInstallerVersion", out var minInstallerVersion);
        toml.TryGetValue("manifestVersion", out var manifestVersion);
        toml.TryGetValue("requirements", out var requirementsObject);
        toml.TryGetValue("download_url", out var downloadUrl);
        toml.TryGetValue("update_url", out var updateUrl);
        toml.TryGetValue("requires_hooks", out var requiresHooksObject);

        var requiresHooksValid = requiresHooksObject is null
                                 || (requiresHooksObject is IList<object?> hookList
                                     && hookList.All(h => h is string));
        var requiresHooks = requiresHooksValid && requiresHooksObject is IList<object?> hooks
            ? hooks.Select(h => ((string)h!).Trim()).ToList()
            : new List<string>();

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
            description?.ToString() ?? "",
            version?.ToString() ?? "",
            minInstallerVersion?.ToString() ?? "0.1",
            manifestVersion?.ToString() ?? "1",
            requirements,
            downloadUrl?.ToString(),
            updateUrl?.ToString(),
            requiresHooks,
            requiresHooksValid,
            localizedNames,
            localizedDescriptions
        );
    }

    private static Dictionary<string, string> ReadLocalizedFields(IEnumerable<KeyValuePair<string, object?>> fields,
        string prefix)
    {
        return fields
            .Where(pair => pair.Key.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)
                           && pair.Value is not null
                           && !string.IsNullOrWhiteSpace(pair.Value.ToString()))
            .ToDictionary(pair => pair.Key[prefix.Length..], pair => pair.Value!.ToString()!,
                StringComparer.OrdinalIgnoreCase);
    }

    private static Dictionary<string, string> ReadLocalizedFields(JObject json, string prefix)
    {
        return json.Properties()
            .Where(property => property.Name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)
                               && property.Value.Type == JTokenType.String
                               && !string.IsNullOrWhiteSpace(property.Value.ToString()))
            .ToDictionary(property => property.Name[prefix.Length..], property => property.Value.ToString(),
                StringComparer.OrdinalIgnoreCase);
    }
}
