namespace Garethp.ModsOfMistriaInstallerLib.Seam;

// The loaded, validated seam catalog. Entries are in application order:
// catalog order plus depends_on edges.
public class SeamCatalog(
    int version,
    IReadOnlyList<SeamEntry> entries,
    IReadOnlyList<HookDeclaration> hookDeclarations,
    IReadOnlyList<CallRewrite> callRewrites)
{
    public int Version { get; } = version;

    public IReadOnlyList<SeamEntry> Entries { get; } = entries;

    public IReadOnlyList<HookDeclaration> HookDeclarations { get; } = hookDeclarations;

    public IReadOnlyList<CallRewrite> CallRewrites { get; } = callRewrites;

    public IReadOnlyList<SeamEntry> Seams => Entries
        .Where(e => e.Kind == SeamEntryKind.Seam)
        .ToList();

    public IReadOnlyList<SeamEntry> EngineFixes => Entries
        .Where(e => e.Kind == SeamEntryKind.EngineFix)
        .ToList();

    // All declared hook names (seam-provided and runtime), sorted
    public IReadOnlyList<string> Hooks => HookDeclarations
        .Select(d => d.Name)
        .Order(StringComparer.Ordinal)
        .ToList();

    // All unique engine files the anchored entries edit, sorted. Call rewrites
    // name no files at catalog time - they touch whatever calls their callee,
    // known only against a pristine tree.
    public IReadOnlyList<string> Files => Entries
        .Select(e => e.File)
        .Distinct()
        .Order(StringComparer.Ordinal)
        .ToList();

    public HookDeclaration? Hook(string name) => HookDeclarations.FirstOrDefault(d => d.Name == name);
}
