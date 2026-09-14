using System.Text;
using Garethp.ModsOfMistriaInstallerLib.Seam;

namespace Garethp.ModsOfMistriaInstallerLib.Operations;

// One call name the framework payload uses that a build neither defines nor
// uses itself. Line is 1-based in the named payload file.
public record FrameworkReadProblem(string Name, string File, int Line);

// Resolves every call name the framework payload uses against one build, since
// the compiler late-binds names and an engine rename there fails only at run
// time. A name resolves when the engine tree defines it, when the engine tree
// calls it, which proves an engine native without a hand-kept list, or when the
// framework defines it. Member calls resolve on their object and bare global
// reads are not calls, so both stay outside the sweep.
public static class FrameworkReads
{
    private static readonly UTF8Encoding Utf8Strict = new(false, true);

    // Keywords that can precede a parenthesis. The engine tree mostly writes
    // statement keywords without parentheses, so tree usage cannot prove them,
    // and `function` itself takes one in the anonymous form.
    private static readonly HashSet<string> Keywords =
    [
        "if", "else", "while", "for", "switch", "repeat", "until", "return",
        "with", "catch", "throw", "do", "not", "and", "or", "new", "delete",
        "function",
    ];

    // The unresolved call names, one report per name at its first call site, in
    // payload file order. `payload` defaults to the embedded framework sources,
    // tests pass their own, and an empty list sweeps nothing.
    public static IReadOnlyList<FrameworkReadProblem> Unresolved(IPristineSource pristine,
        IReadOnlyList<(string Name, string Text)>? payload = null)
    {
        payload ??= PayloadResolver.MmapiSources()
            .Select(s => (s.Name, Utf8Strict.GetString(s.Bytes).Replace("\r\n", "\n")))
            .ToList();

        HashSet<string> known = [];
        foreach (var rel in pristine.GmlFiles())
        {
            var raw = pristine.Read(rel);
            if (raw is null) continue;
            string text;
            try
            {
                text = Utf8Strict.GetString(raw);
            }
            catch (DecoderFallbackException)
            {
                continue;
            }

            CollectCallNames(text.Replace("\r\n", "\n"), known);
        }

        HashSet<string> defined = [];
        foreach (var (_, text) in payload) CollectDefinitionNames(text, defined);

        List<FrameworkReadProblem> problems = [];
        HashSet<string> reported = [];
        foreach (var (file, text) in payload)
        {
            var tokens = GmlScanner.Tokenize(text);
            for (var i = 0; i < tokens.Count; i++)
            {
                if (!IsIdentifier(text, tokens, i)) continue;
                if (!TokenIs(text, tokens, i + 1, "(")) continue;
                if (i > 0 && (TokenIs(text, tokens, i - 1, ".")
                              || TokenIs(text, tokens, i - 1, "function"))) continue;

                var callee = Token(text, tokens[i]);
                if (Keywords.Contains(callee)) continue;
                if (known.Contains(callee) || defined.Contains(callee)) continue;
                if (LineHasExemption(text, tokens[i].Start)) continue;
                if (!reported.Add(callee)) continue;
                problems.Add(new FrameworkReadProblem(callee, file,
                    text.AsSpan(0, tokens[i].Start).Count('\n') + 1));
            }
        }

        return problems;
    }

    // Every identifier applied with a parenthesis, member calls and definitions
    // included. Any name a build uses this way resolves in it.
    private static void CollectCallNames(string text, HashSet<string> known)
    {
        var tokens = GmlScanner.Tokenize(text);
        for (var i = 0; i < tokens.Count; i++)
        {
            if (!IsIdentifier(text, tokens, i)) continue;
            if (!TokenIs(text, tokens, i + 1, "(")) continue;
            known.Add(Token(text, tokens[i]));
        }
    }

    // The names a payload file defines, in the declaration, static, and
    // assignment forms.
    private static void CollectDefinitionNames(string text, HashSet<string> defined)
    {
        var tokens = GmlScanner.Tokenize(text);
        for (var i = 0; i < tokens.Count; i++)
        {
            if (!IsIdentifier(text, tokens, i)) continue;
            if (i > 0 && TokenIs(text, tokens, i - 1, "function")
                && TokenIs(text, tokens, i + 1, "("))
                defined.Add(Token(text, tokens[i]));
            if (TokenIs(text, tokens, i + 1, "=") && TokenIs(text, tokens, i + 2, "function"))
                defined.Add(Token(text, tokens[i]));
        }
    }

    // True when the call's line carries the exemption marker. A capability
    // probe calls a possibly absent name on purpose and opts out where it
    // happens.
    private static bool LineHasExemption(string text, int pos)
    {
        var start = GmlScanner.LineStart(text, pos);
        var end = text.IndexOf('\n', pos);
        if (end == -1) end = text.Length;
        return text.AsSpan(start, end - start).Contains("framework-reads-exempt", StringComparison.Ordinal);
    }

    private static bool IsIdentifier(string text, List<GmlToken> tokens, int i)
    {
        if (i < 0 || i >= tokens.Count) return false;
        var c = text[tokens[i].Start];
        return c is (>= 'a' and <= 'z') or (>= 'A' and <= 'Z') or '_';
    }

    private static bool TokenIs(string text, List<GmlToken> tokens, int i, string value) =>
        i >= 0 && i < tokens.Count && Token(text, tokens[i]) == value;

    private static string Token(string text, GmlToken token) =>
        text[token.Start..token.End];
}
