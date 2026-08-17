namespace Garethp.ModsOfMistriaInstallerLib.ModTypes;

public enum ModFileConflictKind
{
    HardReplacement,
    MergeableMetadata,
    SharedLocalization,
    SharedDestination
}

public sealed record ModFileConflict(
    string Path,
    IReadOnlyList<string> ModIds,
    ModFileConflictKind Kind);

/// <summary>
/// Finds destination-path collisions between selected mods. It is deliberately
/// a read-only check and works for folders, ZIPs and RARs through IMod.
/// </summary>
public static class ModFileConflictDetector
{
    public static IReadOnlyList<ModFileConflict> Find(IEnumerable<IMod> mods)
    {
        var paths = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);
        foreach (var mod in mods)
        {
            foreach (var file in mod.GetAllFiles(""))
            {
                var relative = RelativePath(mod, file);
                if (relative.Length == 0 || IsIgnorableDocumentation(relative) || IsManifest(relative)) continue;
                if (!paths.TryGetValue(relative, out var owners))
                    paths[relative] = owners = new(StringComparer.OrdinalIgnoreCase);
                owners.Add(mod.GetId());
            }
        }

        return paths
            .Where(entry => entry.Value.Count > 1)
            .OrderBy(entry => entry.Key, StringComparer.OrdinalIgnoreCase)
            .Select(entry => new ModFileConflict(
                entry.Key,
                entry.Value.Order(StringComparer.OrdinalIgnoreCase).ToList(),
                Classify(entry.Key)))
            .ToList();
    }

    private static ModFileConflictKind Classify(string path)
    {
        var normalized = path.Replace('\\', '/');

        if (normalized.StartsWith("images/replace/", StringComparison.OrdinalIgnoreCase) ||
            (normalized.StartsWith("animations/ui new/loading screen/", StringComparison.OrdinalIgnoreCase) &&
             normalized.EndsWith(".png", StringComparison.OrdinalIgnoreCase)))
            return ModFileConflictKind.HardReplacement;

        if (normalized.Equals("localization/l10n.meta.toml", StringComparison.OrdinalIgnoreCase) ||
            normalized.Equals("fiddle/ui/text_styles.toml", StringComparison.OrdinalIgnoreCase))
            return ModFileConflictKind.SharedLocalization;

        if (normalized.EndsWith(".meta.toml", StringComparison.OrdinalIgnoreCase) ||
            normalized.StartsWith("fiddle/", StringComparison.OrdinalIgnoreCase))
            return ModFileConflictKind.MergeableMetadata;

        return ModFileConflictKind.SharedDestination;
    }

    private static string RelativePath(IMod mod, string path)
    {
        var normalized = path.Replace('\\', '/').TrimStart('/');
        var basePath = mod.GetBasePath().Replace('\\', '/').TrimEnd('/');
        if (basePath.Length > 0)
        {
            if (normalized.Equals(basePath, StringComparison.OrdinalIgnoreCase)) return "";
            var prefix = basePath + "/";
            if (normalized.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                normalized = normalized[prefix.Length..];
        }
        return normalized;
    }

    private static bool IsManifest(string path) =>
        path.Equals("manifest.json", StringComparison.OrdinalIgnoreCase) ||
        path.Equals("manifest.toml", StringComparison.OrdinalIgnoreCase);

    private static bool IsIgnorableDocumentation(string path)
    {
        var name = Path.GetFileName(path);
        return name.Equals("README.md", StringComparison.OrdinalIgnoreCase) ||
               name.Equals("CHANGELOG.md", StringComparison.OrdinalIgnoreCase) ||
               name.Equals("LICENSE", StringComparison.OrdinalIgnoreCase) ||
               name.StartsWith("LICENSE.", StringComparison.OrdinalIgnoreCase) ||
               name.EndsWith(".txt", StringComparison.OrdinalIgnoreCase);
    }
}
