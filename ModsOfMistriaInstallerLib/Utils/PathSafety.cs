namespace Garethp.ModsOfMistriaInstallerLib.Utils;

// Proves that a data-provided path is safe to join under the install root
// before it reaches any filesystem operation. Catalog `file` values and mod
// gml install paths all pass through here. Returns the problem instead of
// throwing so each caller feeds its own error channel.
public static class PathSafety
{
    // The install-relative root every written path lives under. Catalog
    // `file` values and staged keys are all `assets/...`, which is also how
    // the pristine zip names its entries.
    private const string Root = "assets/";

    // Null when `rel` is safe to join under the install root. `source` names
    // the file and field the path came from.
    public static string? PathProblem(string rel, string source)
    {
        if (string.IsNullOrWhiteSpace(rel)) return $"{source}: empty path";
        if (rel.Contains('\\'))
            return $"{source}: '{rel}' carries a backslash - these paths are posix-style, "
                   + "and a backslash is a path separator on Windows";
        if (rel.Contains(':'))
            // a drive letter ("C:/x") or an NTFS alternate data stream ("x.gml:evil")
            return $"{source}: '{rel}' carries a colon - a drive letter or an alternate "
                   + "data stream, neither of which stays under the install";
        if (rel.StartsWith('/')) return $"{source}: '{rel}' is an absolute path";
        if (rel.Split('/').Any(part => part is "." or ".."))
            return $"{source}: '{rel}' carries a '.' or '..' segment - it could resolve "
                   + "outside the install";
        if (!rel.StartsWith(Root, StringComparison.Ordinal))
            return $"{source}: '{rel}' is not under {Root} - every written path stays "
                   + "inside the assets store";
        return null;
    }
}
