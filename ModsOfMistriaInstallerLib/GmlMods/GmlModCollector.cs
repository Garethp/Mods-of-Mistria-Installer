using Garethp.ModsOfMistriaInstallerLib.ModTypes;

namespace Garethp.ModsOfMistriaInstallerLib.GmlMods;

// Collects a mod's behavioural code surface, or null when it ships no gml/
// tree. All three containers list through GetAllFiles, so the paths come back
// prefixed with the base path and are normalised to mod-relative here.
public static class GmlModCollector
{
    public static GmlModCode? Collect(IMod mod)
    {
        var gmlFiles = mod.GetAllFiles(".gml")
            .Select(path => RelativePath(mod, path))
            .Where(rel => rel.StartsWith("gml/", StringComparison.Ordinal))
            .Order(StringComparer.Ordinal)
            .ToList();
        if (gmlFiles.Count == 0) return null;

        return new GmlModCode(mod, DirName(mod), gmlFiles);
    }

    private static string RelativePath(IMod mod, string absolutePath)
    {
        var normalizedBase = mod.GetBasePath().Replace('\\', '/').TrimEnd('/') + '/';
        var normalizedFull = absolutePath.Replace('\\', '/');
        if (normalizedBase.Length > 1 &&
            normalizedFull.StartsWith(normalizedBase, StringComparison.OrdinalIgnoreCase))
            return normalizedFull[normalizedBase.Length..];
        return normalizedFull;
    }

    // The folder-name namespace: the mod's directory for folder mods, the
    // archive-internal base dir for zip and rar, the symbol when neither names one
    private static string DirName(IMod mod)
    {
        var location = mod.GetLocation().Replace('\\', '/').TrimEnd('/');
        if (location.Length > 0) return location[(location.LastIndexOf('/') + 1)..];

        var basePath = mod.GetBasePath().Replace('\\', '/').Trim('/');
        if (basePath.Length > 0) return basePath[(basePath.LastIndexOf('/') + 1)..];

        return mod.GetId().Replace('.', '_').Replace('-', '_');
    }
}
