namespace Garethp.ModsOfMistriaInstallerLib.Seam;

public enum SeamProblemKind
{
    Anchor,
    Target,
    Wrap,
    Reads,
    Asset,
    Marker,
    Decode,
    MissingFile,
    CallRewrite,
    Framework,
}

public static class SeamProblemKinds
{
    // The fixed wire names the seam check's JSON renders, so the enum spelling
    // and the JSON contract cannot drift apart.
    public static string WireName(this SeamProblemKind kind) => kind switch
    {
        SeamProblemKind.Anchor => "anchor",
        SeamProblemKind.Target => "target",
        SeamProblemKind.Wrap => "wrap",
        SeamProblemKind.Reads => "reads",
        SeamProblemKind.Asset => "asset",
        SeamProblemKind.Marker => "marker",
        SeamProblemKind.Decode => "decode",
        SeamProblemKind.MissingFile => "missing_file",
        SeamProblemKind.CallRewrite => "call_rewrite",
        SeamProblemKind.Framework => "framework",
        _ => throw new ArgumentOutOfRangeException(nameof(kind)),
    };
}

// One staging problem, carrying the structured fields the seam check surfaces
// alongside the human-readable message the batched report lists.
public record SeamProblem(
    string Message,
    SeamProblemKind Kind,
    string EntryId = "",   // seam/fix/rewrite id, "" when not entry-specific
    string File = "",      // engine file rel, "" for tree-wide reports
    string Hint = "",      // the closest-match hint, anchor misses only
    int Line = 0,          // 1-based best-guess pristine line, 0 when unknown
    string Context = "");  // numbered pristine excerpt around that line
