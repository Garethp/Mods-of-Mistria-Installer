namespace Garethp.ModsOfMistriaInstallerLib.Seam;

// One member of a GML enum declaration.
public record GmlEnumMember(
    string Name,
    long Value,        // effective value: the explicit one when stated and parseable, else previous + 1
    string ValueText,  // the explicit value exactly as written, "" when positional
    int Start,         // char offset of the member's name token
    int End)           // char offset just past the member's last token, value included
{
    public bool IsExplicit => ValueText.Length > 0;
}

// One `enum NAME { ... }` declaration found at the top level of a file.
public record GmlEnumScan(
    string Name,
    IReadOnlyList<GmlEnumMember> Members,
    int Start,      // char offset of the `enum` keyword
    int BodyOpen,   // char offset of `{`
    int BodyClose)  // char offset of `}`
{
    public GmlEnumMember? Last => Members.Count > 0 ? Members[^1] : null;
}
