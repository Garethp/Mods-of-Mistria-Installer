namespace Garethp.ModsOfMistriaInstallerLib.ModTypes;

public sealed record DuplicateModGroup(string ModId, IReadOnlyList<IMod> Copies)
{
    public string DisplayName => Copies.FirstOrDefault()?.GetName() ?? ModId;
}

/// <summary>
/// Finds multiple physical sources for the same logical mod. This deliberately
/// only compares manifest identity; it does not inspect every mod file and is
/// therefore cheap enough to use when the user selects a mod.
/// </summary>
public static class DuplicateModDetector
{
    public static IReadOnlyList<DuplicateModGroup> Find(IEnumerable<IMod> mods)
    {
        return mods
            .Where(m => !string.IsNullOrWhiteSpace(m.GetId()))
            .GroupBy(m => m.GetId(), StringComparer.OrdinalIgnoreCase)
            .Where(group => group.Select(m => NormalizeSource(m.GetSourcePath())).Distinct(StringComparer.OrdinalIgnoreCase).Count() > 1)
            .Select(group => new DuplicateModGroup(group.Key, group.ToList()))
            .OrderBy(group => group.DisplayName, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public static string NormalizeSource(string source)
    {
        if (string.IsNullOrWhiteSpace(source)) return "";
        return Path.GetFullPath(source).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
    }
}
