namespace Garethp.ModsOfMistriaInstallerLib.Seam;

// Scans GML source structurally: comment- and string-aware tokens, function
// spans, call sites, global writes, and whitespace-insensitive token anchors.
// Not a parser: a tokenizer plus a brace balancer, which is enough to find a
// named definition exactly once, land a payload inside its body, and know
// where the body ends. Both sides of a match tokenize the same way, so
// matching is whitespace- and comment-insensitive by construction.
public static class GmlScanner
{
    // Tokens are: string literals (one token, quotes included), [A-Za-z0-9_]
    // word runs, and every other non-space character by itself. Line (//) and
    // block comments disappear.
    public static List<GmlToken> Tokenize(string source)
    {
        List<GmlToken> tokens = [];
        var i = 0;
        var n = source.Length;
        while (i < n)
        {
            var c = source[i];
            if (c is ' ' or '\t' or '\r' or '\n')
            {
                i++;
                continue;
            }

            if (c == '/' && i + 1 < n && source[i + 1] == '/')
            {
                var newline = source.IndexOf('\n', i);
                i = newline == -1 ? n : newline;
                continue;
            }

            if (c == '/' && i + 1 < n && source[i + 1] == '*')
            {
                var end = source.IndexOf("*/", i + 2, StringComparison.Ordinal);
                i = end == -1 ? n : end + 2;
                continue;
            }

            if (c == '"')
            {
                // backslash skips the next char; an unterminated string swallows to end of file
                var j = i + 1;
                while (j < n && source[j] != '"') j += source[j] == '\\' ? 2 : 1;
                j = Math.Min(j + 1, n);
                tokens.Add(new GmlToken(i, j));
                i = j;
                continue;
            }

            if (IsWordChar(c))
            {
                var j = i + 1;
                while (j < n && IsWordChar(source[j])) j++;
                tokens.Add(new GmlToken(i, j));
                i = j;
                continue;
            }

            tokens.Add(new GmlToken(i, i + 1));
            i++;
        }

        return tokens;
    }

    // Every definition of `name` in the source, nested ones included.
    public static List<FunctionSpan> FindFunctions(string source, string name, List<GmlToken>? tokens = null)
    {
        tokens ??= Tokenize(source);
        List<FunctionSpan> spans = [];
        for (var i = 0; i < tokens.Count; i++)
        {
            var match = MatchDefinition(source, tokens, i);
            if (match is null || !Text(source, tokens[match.Value.NameIndex]).SequenceEqual(name)) continue;

            var built = BuildSpan(source, tokens, i, match.Value);
            if (built is not null) spans.Add(built.Value.Span);
        }

        return spans;
    }

    // Every definition whose braces sit at the top level of the file. GML
    // hoists top-level `function NAME(` declarations into one flat global
    // namespace regardless of directory: the file's exports. Nested
    // definitions are skipped by jumping past each body, and enum and
    // struct-literal braces are depth-tracked, so a definition inside either
    // never matches.
    public static List<FunctionSpan> TopLevelDefinitions(string source, List<GmlToken>? tokens = null)
    {
        tokens ??= Tokenize(source);
        List<FunctionSpan> spans = [];
        var depth = 0;
        var i = 0;
        while (i < tokens.Count)
        {
            if (depth == 0)
            {
                var match = MatchDefinition(source, tokens, i);
                if (match is not null)
                {
                    var built = BuildSpan(source, tokens, i, match.Value);
                    if (built is not null)
                    {
                        spans.Add(built.Value.Span);
                        i = built.Value.CloseIndex + 1;
                        continue;
                    }
                }
            }

            if (IsChar(source, tokens[i], '{')) depth++;
            else if (IsChar(source, tokens[i], '}')) depth = Math.Max(0, depth - 1);
            i++;
        }

        return spans;
    }

    // Every applied occurrence of `callee`: the exact identifier token whose
    // next significant token is "(". Comments and strings never match (they
    // are not identifier tokens), and a longer identifier the name prefixes
    // never matches (tokens are whole word runs).
    public static List<CallSite> FindCalls(string source, string callee, List<GmlToken>? tokens = null)
    {
        tokens ??= Tokenize(source);
        List<CallSite> sites = [];
        for (var i = 0; i < tokens.Count; i++)
        {
            var token = tokens[i];
            if (!Text(source, token).SequenceEqual(callee)) continue;
            if (i + 1 >= tokens.Count || !IsChar(source, tokens[i + 1], '(')) continue;

            var kind = CallKind.Call;
            if (i > 0 && IsChar(source, tokens[i - 1], '.')) kind = CallKind.Member;
            else if (i > 0 && Text(source, tokens[i - 1]).SequenceEqual("function")) kind = CallKind.Definition;

            var depth = 0;
            var commas = 0;
            var content = false;
            var k = i + 1;
            while (k < tokens.Count)
            {
                var t = tokens[k];
                if (IsOpenBracket(source, t))
                {
                    depth++;
                    if (depth > 1) content = true;
                }
                else if (IsCloseBracket(source, t))
                {
                    depth--;
                    if (depth == 0) break;
                    content = true;
                }
                else
                {
                    content = true;
                    if (depth == 1 && IsChar(source, t, ',')) commas++;
                }

                k++;
            }

            sites.Add(new CallSite(token.Start, token.End, content ? commas + 1 : 0, kind));
        }

        return sites;
    }

    // Every (name, char offset) applied in call position whose identifier
    // starts with one of `prefixes`, excluding member calls and definitions.
    // The same token discipline as FindCalls, for the reserved-namespace lint,
    // which needs a file's whole mmapi_* call surface rather than one callee.
    public static List<(string Name, int Start)> FindPrefixedCalls(string source, IReadOnlyList<string> prefixes,
        List<GmlToken>? tokens = null)
    {
        tokens ??= Tokenize(source);
        List<(string Name, int Start)> found = [];
        for (var i = 0; i < tokens.Count; i++)
        {
            var token = tokens[i];
            var text = Text(source, token);
            var matched = false;
            foreach (var prefix in prefixes)
            {
                if (!text.StartsWith(prefix, StringComparison.Ordinal)) continue;
                matched = true;
                break;
            }

            if (!matched) continue;
            if (i + 1 >= tokens.Count || !IsChar(source, tokens[i + 1], '(')) continue;
            if (i > 0 && (IsChar(source, tokens[i - 1], '.')
                          || Text(source, tokens[i - 1]).SequenceEqual("function"))) continue;

            found.Add((text.ToString(), token.Start));
        }

        return found;
    }

    // Every assignment whose target path starts at `global`: global.NAME,
    // global[$ "NAME"], deeper paths like global.NAME.field[i], and compound
    // assigns (+=, ??=). Comparisons (==, !=) never match. Increments (++/--)
    // are NOT detected: at the token level `global.a - -b` is indistinguishable
    // from a postfix decrement, so they stay a documented blind spot.
    public static List<GlobalWrite> FindGlobalWrites(string source, List<GmlToken>? tokens = null)
    {
        tokens ??= Tokenize(source);
        List<GlobalWrite> writes = [];
        var n = tokens.Count;
        for (var i = 0; i < n; i++)
        {
            if (!Text(source, tokens[i]).SequenceEqual("global")) continue;
            if (i > 0 && IsChar(source, tokens[i - 1], '.')) continue;  // a member named global, not the global scope

            // the root: global.NAME or global[$ "NAME"]
            string root;
            var j = i + 1;
            if (j + 1 < n && IsChar(source, tokens[j], '.') && IsWordChar(source[tokens[j + 1].Start]))
            {
                root = source[tokens[j + 1].Start..tokens[j + 1].End];
                j += 2;
            }
            else if (j + 3 < n && IsChar(source, tokens[j], '[') && IsChar(source, tokens[j + 1], '$')
                     && source[tokens[j + 2].Start] == '"' && IsChar(source, tokens[j + 3], ']'))
            {
                root = Text(source, tokens[j + 2]).Trim('"').ToString();
                j += 4;
            }
            else
            {
                continue;
            }

            // the accessor chain: .field runs and balanced [...] groups
            var bare = true;
            while (j < n)
            {
                if (j + 1 < n && IsChar(source, tokens[j], '.') && IsWordChar(source[tokens[j + 1].Start]))
                {
                    bare = false;
                    j += 2;
                }
                else if (IsChar(source, tokens[j], '['))
                {
                    bare = false;
                    var depth = 0;
                    while (j < n)
                    {
                        if (IsChar(source, tokens[j], '[')) depth++;
                        else if (IsChar(source, tokens[j], ']'))
                        {
                            depth--;
                            if (depth == 0) break;
                        }

                        j++;
                    }

                    j++;
                }
                else
                {
                    break;
                }
            }

            if (j >= n) continue;

            var isWrite =
                (IsChar(source, tokens[j], '=') && !(j + 1 < n && IsChar(source, tokens[j + 1], '=')))
                || (IsCompoundAssign(source, tokens[j]) && j + 1 < n && IsChar(source, tokens[j + 1], '='))
                || (IsChar(source, tokens[j], '?') && j + 1 < n && IsChar(source, tokens[j + 1], '?')
                    && j + 2 < n && IsChar(source, tokens[j + 2], '='));
            if (isWrite) writes.Add(new GlobalWrite(root, tokens[i].Start, bare));
        }

        return writes;
    }

    // Every char span inside the region whose token sequence equals the
    // anchor's. Whitespace- and comment-insensitive on the source side.
    public static List<(int Start, int End)> FindAnchor(string source, int regionStart, int regionEnd,
        string anchor, List<GmlToken>? tokens = null)
    {
        tokens ??= Tokenize(source);
        var want = Tokenize(anchor);
        if (want.Count == 0) return [];

        var window = tokens
            .Where(t => t.Start >= regionStart && t.End <= regionEnd)
            .ToList();
        List<(int Start, int End)> spans = [];
        for (var i = 0; i <= window.Count - want.Count; i++)
        {
            var all = true;
            for (var k = 0; k < want.Count; k++)
            {
                if (Text(source, window[i + k]).SequenceEqual(Text(anchor, want[k]))) continue;
                all = false;
                break;
            }

            if (all) spans.Add((window[i].Start, window[i + want.Count - 1].End));
        }

        return spans;
    }

    // The char offset of the start of the line containing `pos`.
    public static int LineStart(string source, int pos) =>
        pos <= 0 ? 0 : source.LastIndexOf('\n', pos - 1) + 1;

    // The char offset just past the newline ending the line containing `pos`.
    public static int NextLineStart(string source, int pos)
    {
        var newline = source.IndexOf('\n', pos);
        return newline == -1 ? source.Length : newline + 1;
    }

    // The leading whitespace of the line containing `pos`.
    public static string LineIndent(string source, int pos)
    {
        var start = LineStart(source, pos);
        var end = start;
        while (end < source.Length && source[end] is ' ' or '\t') end++;
        return source[start..end];
    }

    // (form, name token index, paren token index) when a definition starts at token i
    private static (FunctionForm Form, int NameIndex, int ParenIndex)? MatchDefinition(
        string source, List<GmlToken> tokens, int i)
    {
        bool TokenIs(int k, string text) => k < tokens.Count && Text(source, tokens[k]).SequenceEqual(text);
        bool StartsWord(int k) => k < tokens.Count && IsWordChar(source[tokens[k].Start]);

        if (TokenIs(i, "function") && TokenIs(i + 2, "(") && StartsWord(i + 1))
            return (FunctionForm.Decl, i + 1, i + 2);
        if (TokenIs(i, "static") && TokenIs(i + 2, "=") && TokenIs(i + 3, "function") && TokenIs(i + 4, "("))
            return (FunctionForm.Static, i + 1, i + 4);
        if (TokenIs(i + 1, "=") && TokenIs(i + 2, "function") && TokenIs(i + 3, "(") && StartsWord(i)
            && (i == 0 || !TokenIs(i - 1, "static")))  // the static form owns that match
            return (FunctionForm.Assign, i, i + 3);
        return null;
    }

    // The FunctionSpan for the definition starting at token i, plus the token
    // index of its closing brace. Null when the definition is unterminated.
    private static (FunctionSpan Span, int CloseIndex)? BuildSpan(
        string source, List<GmlToken> tokens, int i, (FunctionForm Form, int NameIndex, int ParenIndex) match)
    {
        var (form, nameIndex, parenIndex) = match;

        // the parameter list: walk to the matching close paren
        var depth = 0;
        var j = parenIndex;
        while (j < tokens.Count)
        {
            if (IsChar(source, tokens[j], '(')) depth++;
            else if (IsChar(source, tokens[j], ')'))
            {
                depth--;
                if (depth == 0) break;
            }

            j++;
        }

        if (j >= tokens.Count) return null;
        var paramsClose = j;
        var paramsText = source[tokens[parenIndex].End..tokens[paramsClose].Start];

        List<string> args = [];
        depth = 0;
        var expecting = true;
        for (var k = parenIndex + 1; k < paramsClose; k++)
        {
            var token = tokens[k];
            if (IsOpenBracket(source, token)) depth++;
            else if (IsCloseBracket(source, token)) depth--;
            else if (depth == 0 && IsChar(source, token, ',')) expecting = true;
            else if (depth == 0 && expecting && IsWordChar(source[token.Start]))
            {
                args.Add(source[token.Start..token.End]);
                expecting = false;
            }
        }

        // the body: the first brace after the parameter list (skips the
        // `: Parent(...) constructor` run), then balance to its close
        j = paramsClose + 1;
        while (j < tokens.Count && !IsChar(source, tokens[j], '{')) j++;
        if (j >= tokens.Count) return null;
        var bodyOpenIndex = j;
        depth = 0;
        while (j < tokens.Count)
        {
            if (IsChar(source, tokens[j], '{')) depth++;
            else if (IsChar(source, tokens[j], '}'))
            {
                depth--;
                if (depth == 0) break;
            }

            j++;
        }

        if (j >= tokens.Count) return null;

        var span = new FunctionSpan(
            Name: source[tokens[nameIndex].Start..tokens[nameIndex].End],
            Form: form,
            Start: tokens[i].Start,
            NameStart: tokens[nameIndex].Start,
            NameEnd: tokens[nameIndex].End,
            BodyOpen: tokens[bodyOpenIndex].Start,
            BodyClose: tokens[j].Start,
            Params: paramsText,
            Args: args);
        return (span, j);
    }

    private static ReadOnlySpan<char> Text(string source, GmlToken token) =>
        source.AsSpan(token.Start, token.End - token.Start);

    private static bool IsChar(string source, GmlToken token, char c) =>
        token.End - token.Start == 1 && source[token.Start] == c;

    private static bool IsOpenBracket(string source, GmlToken token) =>
        token.End - token.Start == 1 && source[token.Start] is '(' or '[' or '{';

    private static bool IsCloseBracket(string source, GmlToken token) =>
        token.End - token.Start == 1 && source[token.Start] is ')' or ']' or '}';

    private static bool IsCompoundAssign(string source, GmlToken token) =>
        token.End - token.Start == 1 && source[token.Start] is '+' or '-' or '*' or '/' or '%' or '|' or '&' or '^';

    private static bool IsWordChar(char c) =>
        c is (>= 'a' and <= 'z') or (>= 'A' and <= 'Z') or (>= '0' and <= '9') or '_';
}
