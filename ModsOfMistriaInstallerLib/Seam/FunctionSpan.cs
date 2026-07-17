namespace Garethp.ModsOfMistriaInstallerLib.Seam;

// The three definition forms the engine source actually uses:
// "function f() {" (decl), "static f = function() {" (static),
// "f = function() {" (assign, self.f included).
public enum FunctionForm
{
    Decl,
    Static,
    Assign,
}

public record FunctionSpan(
    string Name,
    FunctionForm Form,
    int Start,      // char offset of the definition's first token
    int NameStart,  // char offset of the NAME token (the rename point for wraps)
    int NameEnd,
    int BodyOpen,   // char offset of the opening brace
    int BodyClose,  // char offset of the closing brace
    string Params,  // the raw parameter list text, defaults included
    IReadOnlyList<string> Args);  // parameter names only
