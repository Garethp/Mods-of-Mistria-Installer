using System.Text.RegularExpressions;
using Garethp.ModsOfMistriaInstallerLib.ModTypes;

namespace Garethp.ModsOfMistriaInstallerLib.GmlMods;

public sealed record ModHotkeyUsage(string Key, string ModId, string Source, bool Rebindable);

public sealed record ModHotkeyConflict(string Key, IReadOnlyList<ModHotkeyUsage> Usages);

/// <summary>
/// Finds likely keyboard conflicts in GML mods. This is intentionally a
/// warning-only, conservative scan: many mods make their bindings configurable
/// at runtime, so the result must never block installation.
/// </summary>
public static class HotkeyConflictDetector
{
    private static readonly Regex MacroKey = new(
        "#macro\\s+\\w+\\s+\\\"(F(?:[1-9]|1[0-2]))\\\"",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex DirectVirtualKey = new(
        @"\bvk_(f(?:[1-9]|1[0-2]))\b",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public static IReadOnlyList<ModHotkeyConflict> Find(IEnumerable<IMod> mods)
    {
        var usages = new List<ModHotkeyUsage>();

        foreach (var mod in mods)
        {
            foreach (var path in mod.GetAllFiles(".gml"))
            {
                var relative = RelativePath(mod, path);
                if (!relative.StartsWith("gml/", StringComparison.OrdinalIgnoreCase)) continue;

                string source;
                try { source = mod.ReadFile(relative); }
                catch { continue; }

                var keys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                foreach (Match match in MacroKey.Matches(source))
                    keys.Add(match.Groups[1].Value.ToUpperInvariant());
                foreach (Match match in DirectVirtualKey.Matches(source))
                    keys.Add(match.Groups[1].Value.ToUpperInvariant());

                // Auxiliary Bag generates its default bindings dynamically,
                // so the key names are not present as vk_f1 ... vk_f7 literals.
                if (source.Contains("mah_default_hotkey_name", StringComparison.OrdinalIgnoreCase) &&
                    source.Contains("mah_hotkey_slot_7", StringComparison.OrdinalIgnoreCase))
                {
                    for (var i = 1; i <= 7; i++) keys.Add($"F{i}");
                }

                foreach (var key in keys)
                {
                    var rebindable = source.Contains("mmapi_hotkey_register", StringComparison.OrdinalIgnoreCase) ||
                                     source.Contains("hotkey", StringComparison.OrdinalIgnoreCase) &&
                                     source.Contains("config", StringComparison.OrdinalIgnoreCase);
                    usages.Add(new ModHotkeyUsage(key, mod.GetId(), relative, rebindable));
                }
            }
        }

        return usages
            .GroupBy(usage => usage.Key, StringComparer.OrdinalIgnoreCase)
            .Where(group => group.Select(usage => usage.ModId).Distinct(StringComparer.OrdinalIgnoreCase).Count() > 1)
            .OrderBy(group => group.Key, StringComparer.OrdinalIgnoreCase)
            .Select(group => new ModHotkeyConflict(
                group.Key,
                group.GroupBy(usage => usage.ModId, StringComparer.OrdinalIgnoreCase)
                    .Select(owner => owner.First())
                    .OrderBy(usage => usage.ModId, StringComparer.OrdinalIgnoreCase)
                    .ToList()))
            .ToList();
    }

    private static string RelativePath(IMod mod, string path)
    {
        var normalizedBase = mod.GetBasePath().Replace('\\', '/').TrimEnd('/') + "/";
        var normalizedFull = path.Replace('\\', '/');
        if (normalizedBase.Length > 1 &&
            normalizedFull.StartsWith(normalizedBase, StringComparison.OrdinalIgnoreCase))
            return normalizedFull[normalizedBase.Length..];
        return normalizedFull;
    }
}
