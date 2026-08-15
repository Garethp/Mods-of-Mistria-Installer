namespace Garethp.ModsOfMistriaInstallerLib.Seam;

public enum SeamProblemKind
{
    Anchor,
    Target,
    Wrap,
    Marker,
    Decode,
    MissingFile,
    CallRewrite,

    // Reserved for extension failures where our state is wrong, such as ordinal
    // collision, ordinal gap, vacancy path collision. Extension failures that
    // share a class with a seam failure reuse that class instead. An anchor
    // that stopped matching is Anchor whether a seam or an extension site
    // owned it, so a consumer filtering on "anchor" catches extension rot too.
    // The split a consumer acts on is "the game changed" vs "your
    // configuration is wrong", not which subsystem reported it.
    Extension,
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
        SeamProblemKind.Marker => "marker",
        SeamProblemKind.Decode => "decode",
        SeamProblemKind.MissingFile => "missing_file",
        SeamProblemKind.CallRewrite => "call_rewrite",
        SeamProblemKind.Extension => "extension",
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
