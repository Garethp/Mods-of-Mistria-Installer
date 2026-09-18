namespace Garethp.ModsOfMistriaInstallerLib.Seam;

// The scope reads of a rendered dispatch payload, every bare identifier the
// injected code resolves against the enclosing function at run time. GML
// late-binds such names, so a payload reading a variable the engine no longer
// declares still applies and compiles, and the stager holds these reads against
// the target function's span the same way it holds anchors.
public static class PayloadReads
{
    // Engine asset names by naming convention. They resolve from the asset
    // table, not from any GML scope, so no span or file can prove them.
    private static readonly string[] AssetPrefixes = ["spr_", "obj_", "rm_"];

    // Names the language resolves without the enclosing function's scope.
    private static readonly HashSet<string> Keywords =
    [
        "if", "else", "return", "var", "try", "catch", "finally", "throw",
        "function", "new", "delete", "self", "other", "global", "undefined",
        "true", "false", "noone", "all", "and", "or", "not", "xor", "mod",
        "div", "while", "do", "until", "repeat", "for", "switch", "case",
        "default", "break", "continue", "exit", "with", "enum", "static",
        "constructor", "globalvar",
    ];

    // The reads, in payload order, deduplicated. A word token is a scope read
    // unless something else binds it first. Keywords, uppercase engine globals,
    // asset-prefixed names, the framework's own mmapi names, member names, call
    // names, struct keys, and names the payload declares through `var` or a
    // catch clause all bind elsewhere.
    public static List<string> ScopeReads(string payload)
    {
        var tokens = GmlScanner.Tokenize(payload);

        HashSet<string> declared = [];
        for (var i = 0; i < tokens.Count; i++)
        {
            if (TokenIs(payload, tokens, i, "var") && IsIdentifier(payload, tokens, i + 1))
                declared.Add(Text(payload, tokens[i + 1]));
            if (TokenIs(payload, tokens, i, "catch") && TokenIs(payload, tokens, i + 1, "(")
                && IsIdentifier(payload, tokens, i + 2))
                declared.Add(Text(payload, tokens[i + 2]));
        }

        List<string> reads = [];
        for (var i = 0; i < tokens.Count; i++)
        {
            if (!IsIdentifier(payload, tokens, i)) continue;
            var name = Text(payload, tokens[i]);
            if (Keywords.Contains(name)) continue;
            if (name.Any(char.IsUpper)) continue;
            if (name.StartsWith("mmapi_", StringComparison.Ordinal)
                || name.StartsWith("__mmapi_", StringComparison.Ordinal)) continue;
            if (AssetPrefixes.Any(p => name.StartsWith(p, StringComparison.Ordinal))) continue;
            if (i > 0 && TokenIs(payload, tokens, i - 1, ".")) continue;
            if (TokenIs(payload, tokens, i + 1, "(")) continue;
            // A bare assignment target creates the member it writes, so that
            // occurrence is not a read. Compound assigns and comparisons still
            // read, and other occurrences of the same name still count.
            if (TokenIs(payload, tokens, i + 1, "=") && !TokenIs(payload, tokens, i + 2, "=")) continue;
            if (TokenIs(payload, tokens, i + 1, ":") && i > 0
                && (TokenIs(payload, tokens, i - 1, "{") || TokenIs(payload, tokens, i - 1, ","))) continue;
            if (declared.Contains(name)) continue;
            if (!reads.Contains(name)) reads.Add(name);
        }

        return reads;
    }

    // The asset names a payload references in value position, meaning sprite,
    // object, and room identifiers. ScopeReads exempts them because no GML
    // scope can prove them, and this collects them for the check against a
    // build's asset inventory.
    public static List<string> AssetReads(string payload)
    {
        var tokens = GmlScanner.Tokenize(payload);
        List<string> reads = [];
        for (var i = 0; i < tokens.Count; i++)
        {
            if (!IsIdentifier(payload, tokens, i)) continue;
            var name = Text(payload, tokens[i]);
            if (!AssetPrefixes.Any(p => name.StartsWith(p, StringComparison.Ordinal))) continue;
            if (i > 0 && TokenIs(payload, tokens, i - 1, ".")) continue;
            if (TokenIs(payload, tokens, i + 1, "(")) continue;
            if (TokenIs(payload, tokens, i + 1, "=") && !TokenIs(payload, tokens, i + 2, "=")) continue;
            if (TokenIs(payload, tokens, i + 1, ":") && i > 0
                && (TokenIs(payload, tokens, i - 1, "{") || TokenIs(payload, tokens, i - 1, ","))) continue;
            if (!reads.Contains(name)) reads.Add(name);
        }

        return reads;
    }

    // The names a payload brings into being through `var` declarations, catch
    // variables, and bare assignment targets, which create the member they
    // write. A seam may read a name a sibling seam in the same file declares,
    // in either staging order, so the stager consults these across a file's
    // whole entry group.
    public static HashSet<string> Declarations(string payload)
    {
        var tokens = GmlScanner.Tokenize(payload);
        HashSet<string> declared = [];
        for (var i = 0; i < tokens.Count; i++)
        {
            if (TokenIs(payload, tokens, i, "var") && IsIdentifier(payload, tokens, i + 1))
                declared.Add(Text(payload, tokens[i + 1]));
            if (TokenIs(payload, tokens, i, "catch") && TokenIs(payload, tokens, i + 1, "(")
                && IsIdentifier(payload, tokens, i + 2))
                declared.Add(Text(payload, tokens[i + 2]));
            if (IsIdentifier(payload, tokens, i) && TokenIs(payload, tokens, i + 1, "=")
                && !TokenIs(payload, tokens, i + 2, "=")
                && !(i > 0 && TokenIs(payload, tokens, i - 1, ".")))
                declared.Add(Text(payload, tokens[i]));
        }

        return declared;
    }

    // A word token whose first character can start a GML identifier. Word
    // runs starting with a digit are numeric literals.
    private static bool IsIdentifier(string payload, List<GmlToken> tokens, int i)
    {
        if (i < 0 || i >= tokens.Count) return false;
        var c = payload[tokens[i].Start];
        return c is (>= 'a' and <= 'z') or (>= 'A' and <= 'Z') or '_';
    }

    private static bool TokenIs(string payload, List<GmlToken> tokens, int i, string text) =>
        i >= 0 && i < tokens.Count && Text(payload, tokens[i]) == text;

    private static string Text(string payload, GmlToken token) =>
        payload[token.Start..token.End];
}
