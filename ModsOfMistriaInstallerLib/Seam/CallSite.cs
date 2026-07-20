namespace Garethp.ModsOfMistriaInstallerLib.Seam;

// Call → a direct call a rewrite may touch; Member → x.callee(...), resolved
// against a struct; Definition → function callee(...).
public enum CallKind
{
    Call,
    Member,
    Definition,
}

// One applied occurrence of a callee: the exact identifier token whose next
// significant token is "(". Args counts top-level commas across all bracket
// kinds, so a nested call, array or struct literal inside an argument is one.
public record CallSite(int NameStart, int NameEnd, int Args, CallKind Kind);
