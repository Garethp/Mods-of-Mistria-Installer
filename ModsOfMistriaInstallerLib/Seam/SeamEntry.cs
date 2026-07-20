namespace Garethp.ModsOfMistriaInstallerLib.Seam;

// A [[seam]] feeds one or more declared hooks; an [[engine_fix]] is a plain
// engine edit with no hooks. Both apply identically.
public enum SeamEntryKind
{
    Seam,
    EngineFix,
}

public static class SeamEntryKinds
{
    public static string CatalogName(this SeamEntryKind kind) => kind switch
    {
        SeamEntryKind.Seam => "seam",
        SeamEntryKind.EngineFix => "engine_fix",
        _ => throw new ArgumentOutOfRangeException(nameof(kind)),
    };
}

// One anchored engine edit. The anchor must match the pristine file exactly
// once, else staging fails closed. Anchor and replace are \n-normalised.
public record SeamEntry(
    string Id,                        // [[seam]].id or [[engine_fix]].name
    SeamEntryKind Kind,
    string File,                      // entry path, normalised to "assets/gml/..."
    string Anchor,                    // pristine snippet
    string Replace,                   // seamed snippet
    string Marker,                    // identity token; must appear in Replace
    IReadOnlyList<string> Hooks,      // provides[] hook names, [[seam]] only
    IReadOnlyList<string> DependsOn,  // entry ids that must apply before this one
    DispatchOp? Op,                   // template-form seams only, null for text form
    string TargetFn,                  // target-form: the function the payload lands in
    string TargetAt,                  // target-form: "head" | "before" | "after" ("" for wrap)
    string TargetAnchor);             // target-form: the token anchor for before/after
