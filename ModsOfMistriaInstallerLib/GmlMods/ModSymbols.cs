namespace Garethp.ModsOfMistriaInstallerLib.GmlMods;

// The first (preferring bare) write to one global root. Bare means some write
// replaces the root itself (global.x = ...); a deeper write (global.x.f = ...)
// mutates its contents without replacing it.
public record GlobalRoot(string File, int Line, bool Bare);

// One mod's language-level symbol surface, scanned from its gml text: the
// top-level function names GML hoists into the flat global namespace, the
// repeats among them (a duplicate export within one mod bricks the boot like a
// cross-mod one), and the global roots the mod writes.
public class ModSymbols(string modId)
{
    public string ModId { get; } = modId;

    // function name → (file, 1-based line) of its first definition
    public Dictionary<string, (string File, int Line)> Functions { get; } = [];

    // function name → the repeat sites after the first
    public Dictionary<string, List<(string File, int Line)>> Duplicates { get; } = [];

    public Dictionary<string, GlobalRoot> GlobalRoots { get; } = [];
}
