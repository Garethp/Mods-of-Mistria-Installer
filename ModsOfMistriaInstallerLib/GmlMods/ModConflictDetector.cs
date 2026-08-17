using Garethp.ModsOfMistriaInstallerLib.ModTypes;
using Garethp.ModsOfMistriaInstallerLib.Seam;

namespace Garethp.ModsOfMistriaInstallerLib.GmlMods;

public sealed record ModConflict(string Key, IReadOnlyList<string> ModIds, string Description);

/// <summary>Read-only conflict checks for the currently selected GML mods.</summary>
public static class ModConflictDetector
{
    public static IReadOnlyList<ModConflict> Find(IEnumerable<IMod> mods, SeamCatalog? catalog = null)
    {
        var codes = mods.Select(GmlModCollector.Collect).OfType<GmlModCode>().ToList();
        if (codes.Count < 2) return [];

        catalog ??= LoadCatalog();
        return GmlModLint.LintHooks(codes, catalog)
            .Where(f => f.File.Length == 0 && f.Message.Contains("exclusive", StringComparison.OrdinalIgnoreCase))
            .Select(f => new ModConflict(HookKey(f.Message), ExtractModIds(f.Message), f.Message))
            .GroupBy(c => c.Key, StringComparer.OrdinalIgnoreCase)
            .Select(g => g.First())
            .ToList();
    }

    private static string HookKey(string message)
    {
        const string prefix = "hook '";
        var start = message.IndexOf(prefix, StringComparison.Ordinal);
        if (start < 0) return message;
        start += prefix.Length;
        var end = message.IndexOf('\'', start);
        return end < 0 ? message[start..] : message[start..end];
    }

    private static IReadOnlyList<string> ExtractModIds(string message)
    {
        var open = message.IndexOf('(');
        var close = message.LastIndexOf(')');
        if (open < 0 || close <= open) return [];
        return message[(open + 1)..close]
            .Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
    }

    private static SeamCatalog LoadCatalog()
    {
        var (name, bytes) = PayloadResolver.SeamCatalog();
        return SeamCatalogLoader.Load(bytes, name);
    }
}
