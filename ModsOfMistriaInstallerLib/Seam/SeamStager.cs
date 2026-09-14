using System.Text;
using System.Text.RegularExpressions;

namespace Garethp.ModsOfMistriaInstallerLib.Seam;

// The whole stage: seamed and rewritten engine files, plus the generated hook
// catalog. The hook catalog stays out of Files because it is an ADDED file,
// not a seamed one; its destination is SeamStager.HookCatalogRel.
public class StageResult(IReadOnlyDictionary<string, StagedFile> files, string hookCatalogGml)
{
    public IReadOnlyDictionary<string, StagedFile> Files { get; } = files;

    public string HookCatalogGml { get; } = hookCatalogGml;
}

// One staging problem in flight: the stager throws this internally, and the
// staging loops catch, accumulate and re-throw the batch as SeamStagingException.
internal class SeamProblemException(SeamProblem problem) : Exception(problem.Message)
{
    public SeamProblem Problem { get; } = problem;
}

// Stages the whole catalog in memory against pristine bytes. Nothing is
// written. Fail-closed and batched: every anchor miss, decode failure and
// marker collision across the whole catalog is reported in one error.
public static class SeamStager
{
    public const string HookCatalogRel = "assets/gml/scripts/mmapi/mmapi_hook_catalog.gml";

    // The framework's destination prefix. The trailing slash is load-bearing:
    // the delivery, the gate's shared set, the export scan and the call-rewrite
    // exclusion all match it, and a mod whose namespace merely starts with
    // mmapi is not the framework.
    public const string MmapiTreePrefix = "assets/gml/scripts/mmapi/";

    private static readonly Regex WhitespaceRuns = new(@"\s+");

    private static readonly UTF8Encoding Utf8Strict = new(false, true);

    // Simulate, then the call rewrites over the whole engine tree, then the
    // generated hook catalog. Problems from both phases land in one batched
    // report, so seam drift does not hide rewrite drift in the same build.
    public static StageResult StageAll(SeamCatalog catalog, IPristineSource pristine)
    {
        List<SeamProblem> problems = [];
        var staged = SimulateCore(catalog, pristine, problems);
        StageCallRewritesCore(catalog, staged, pristine, pristine.GmlFiles(), problems);
        if (problems.Count > 0) throw new SeamStagingException("seam staging failed", problems);
        return new StageResult(staged, HookCatalogRenderer.Render(catalog));
    }

    // Stage every catalog entry in memory, sourced from pristine. Marker
    // discipline: a marker found in the pristine file, or in the staged text
    // before its own entry applies, cannot identify this entry's edit, so both
    // are errors. Every apply re-derives from pristine, so markers are
    // identity, not idempotency.
    public static Dictionary<string, StagedFile> Simulate(SeamCatalog catalog, IPristineSource pristine)
    {
        List<SeamProblem> problems = [];
        var staged = SimulateCore(catalog, pristine, problems);
        if (problems.Count > 0) throw new SeamStagingException("seam staging failed", problems);
        return staged;
    }

    private static Dictionary<string, StagedFile> SimulateCore(SeamCatalog catalog,
        IPristineSource pristine, List<SeamProblem> problems)
    {
        Dictionary<string, StagedFile> staged = [];

        // When the source carries an asset inventory, every sprite, room, and
        // object identifier a payload references must be in it.
        var assetNames = pristine.AssetNames();

        // The names every payload in the same file declares. A seam may read a
        // name a sibling seam introduces, and the reader can stage before the
        // declarer, so the scope-read checks consult the whole group.
        Dictionary<string, List<(string Id, HashSet<string> Declared)>> declaredByFile = [];
        foreach (var entry in catalog.Entries)
        {
            if (!declaredByFile.TryGetValue(entry.File, out var group))
            {
                group = [];
                declaredByFile[entry.File] = group;
            }

            group.Add((entry.Id, PayloadReads.Declarations(entry.Replace)));
        }

        foreach (var entry in catalog.Entries)
        {
            var current = staged.GetValueOrDefault(entry.File);
            if (current is null)
            {
                var raw = pristine.Read(entry.File);
                if (raw is null)
                {
                    problems.Add(new SeamProblem(
                        $"{entry.Kind.CatalogName()} '{entry.Id}': seam target not found in "
                        + $"pristine source: {entry.File}",
                        SeamProblemKind.MissingFile, entry.Id, entry.File));
                    continue;
                }

                string text;
                try
                {
                    text = Utf8Strict.GetString(raw);
                }
                catch (DecoderFallbackException exception)
                {
                    problems.Add(new SeamProblem(
                        $"{entry.Kind.CatalogName()} '{entry.Id}': pristine {entry.File} is not "
                        + $"UTF-8 ({exception.Message})",
                        SeamProblemKind.Decode, entry.Id, entry.File));
                    continue;
                }

                var eol = text.Contains("\r\n", StringComparison.Ordinal) ? "\r\n" : "\n";
                current = new StagedFile(Norm(text), eol);
                if (current.Text.Contains(entry.Marker, StringComparison.Ordinal))
                {
                    problems.Add(new SeamProblem(
                        $"{entry.Kind.CatalogName()} '{entry.Id}': marker '{entry.Marker}' already "
                        + $"appears in pristine {entry.File} - pick a distinctive marker",
                        SeamProblemKind.Marker, entry.Id, entry.File));
                    staged[entry.File] = current;
                    continue;
                }

                staged[entry.File] = current;
            }
            else if (current.Text.Contains(entry.Marker, StringComparison.Ordinal))
            {
                problems.Add(new SeamProblem(
                    $"{entry.Kind.CatalogName()} '{entry.Id}': marker '{entry.Marker}' already "
                    + $"appears in the staged {entry.File} (applied: "
                    + $"{string.Join(", ", current.EntryIds)}) - pick a distinctive marker "
                    + "or fix depends_on",
                    SeamProblemKind.Marker, entry.Id, entry.File));
                continue;
            }

            if (assetNames is not null)
            {
                var missingAssets = PayloadReads.AssetReads(entry.Replace)
                    .Where(name => !assetNames.Contains(name))
                    .ToList();
                if (missingAssets.Count > 0)
                {
                    var names = string.Join(", ", missingAssets.Select(name => $"'{name}'"));
                    problems.Add(new SeamProblem(
                        $"{entry.Kind.CatalogName()} '{entry.Id}': the payload references "
                        + (missingAssets.Count == 1 ? "asset " : "assets ") + names
                        + " that this build does not carry - the engine changed; "
                        + "the seam catalog needs updating",
                        SeamProblemKind.Asset, entry.Id, entry.File));
                    continue;
                }
            }

            try
            {
                HashSet<string> siblingDeclared = [];
                foreach (var (id, declared) in declaredByFile[entry.File])
                    if (id != entry.Id) siblingDeclared.UnionWith(declared);
                current.Text = ApplySeam(entry, current.Text, siblingDeclared);
                current.AppliedIds.Add(entry.Id);
            }
            catch (SeamProblemException exception)
            {
                problems.Add(exception.Problem);
            }
        }

        return staged;
    }

    // Apply one catalog entry to \n-normalised text and return the result.
    // Fail-closed, so the anchor must match exactly once. `siblingDeclared`
    // carries the names other entries in the same file declare, which the
    // scope-read checks accept as bound.
    public static string ApplySeam(SeamEntry entry, string text,
        IReadOnlySet<string>? siblingDeclared = null)
    {
        if (entry.TargetFn.Length > 0) return ApplyTarget(entry, text, siblingDeclared);

        var occurrences = CountOccurrences(text, entry.Anchor);
        if (occurrences != 1)
        {
            var hint = AnchorHint(entry.Anchor, text);
            var (line, context) = ClosestContext(entry.Anchor, text);
            throw new SeamProblemException(new SeamProblem(
                $"{entry.Kind.CatalogName()} '{entry.Id}': anchor matched {occurrences}x in {entry.File} "
                + "(expected 1) - the engine file changed; the seam catalog needs updating"
                + (hint.Length > 0 ? $" ({hint})" : ""),
                SeamProblemKind.Anchor, entry.Id, entry.File, hint, line, context));
        }

        CheckAnchorPayloadReads(entry, text, siblingDeclared);

        return text.Replace(entry.Anchor, entry.Replace, StringComparison.Ordinal);
    }

    // The text-form twin of the target-form scope-read check. The anchor proves
    // only the lines it quotes, so the reads are held against the function
    // enclosing the match, widened to the anchor's extent, or against the whole
    // file when the match sits at the top level.
    private static void CheckAnchorPayloadReads(SeamEntry entry, string text,
        IReadOnlySet<string>? siblingDeclared)
    {
        var reads = PayloadReads.ScopeReads(entry.Replace);
        if (reads.Count == 0) return;

        var matchStart = text.IndexOf(entry.Anchor, StringComparison.Ordinal);
        var matchEnd = matchStart + entry.Anchor.Length;
        var tokens = GmlScanner.Tokenize(text);
        var span = GmlScanner.EnclosingFunction(text, matchStart, tokens);
        var regionStart = span is null ? 0 : Math.Min(span.Start, matchStart);
        var regionEnd = span is null ? text.Length : Math.Max(span.BodyClose, matchEnd);

        var missing = reads
            .Where(name => !(siblingDeclared?.Contains(name) ?? false))
            .Where(name => !Mentions(text, tokens, regionStart, regionEnd, name))
            .ToList();
        if (missing.Count == 0) return;

        var where = span is null ? entry.File : $"'{span.Name}' in {entry.File}";
        var names = string.Join(", ", missing.Select(name => $"'{name}'"));
        var line = CountLines(text, matchStart);
        throw new SeamProblemException(new SeamProblem(
            $"{entry.Kind.CatalogName()} '{entry.Id}': the payload reads {names} but "
            + where + " never mentions "
            + (missing.Count == 1 ? "it" : "them")
            + " - the engine file changed; the seam catalog needs updating",
            SeamProblemKind.Reads, entry.Id, entry.File,
            Line: line, Context: NumberedExcerpt(text, line)));
    }

    // Stage every [[call_rewrite]] across the engine tree, in place over the
    // Simulate result. Runs after every anchored edit, so anchors keep
    // matching pristine text and call tokens inside seam-injected text are
    // rewritten too. Files under scripts/mmapi/ are excluded: the wrapper
    // must keep its native call.
    public static void StageCallRewrites(SeamCatalog catalog, Dictionary<string, StagedFile> staged,
        IPristineSource pristine, IReadOnlyList<string> gmlFiles)
    {
        List<SeamProblem> problems = [];
        StageCallRewritesCore(catalog, staged, pristine, gmlFiles, problems);
        if (problems.Count > 0) throw new SeamStagingException("call-rewrite staging failed", problems);
    }

    private static void StageCallRewritesCore(SeamCatalog catalog, Dictionary<string, StagedFile> staged,
        IPristineSource pristine, IReadOnlyList<string> gmlFiles, List<SeamProblem> problems)
    {
        if (catalog.CallRewrites.Count == 0) return;
        var priorProblems = problems.Count;
        var siteCounts = catalog.CallRewrites.ToDictionary(r => r.Id, _ => 0);
        var fileCounts = catalog.CallRewrites.ToDictionary(r => r.Id, _ => 0);

        var sweep = gmlFiles
            .Union(staged.Keys)
            .Order(StringComparer.Ordinal)
            .ToList();
        foreach (var rel in sweep)
        {
            if (rel.StartsWith(MmapiTreePrefix, StringComparison.Ordinal)) continue;

            var current = staged.GetValueOrDefault(rel);
            string text;
            string eol;
            if (current is not null)
            {
                text = current.Text;
                eol = current.Eol;
            }
            else
            {
                var raw = pristine.Read(rel);
                if (raw is null)
                {
                    problems.Add(new SeamProblem(
                        $"call_rewrite: listed gml file not found in pristine source: {rel}",
                        SeamProblemKind.MissingFile, File: rel));
                    continue;
                }

                string decoded;
                try
                {
                    decoded = Utf8Strict.GetString(raw);
                }
                catch (DecoderFallbackException exception)
                {
                    problems.Add(new SeamProblem(
                        $"call_rewrite: pristine {rel} is not UTF-8 ({exception.Message})",
                        SeamProblemKind.Decode, File: rel));
                    continue;
                }

                eol = decoded.Contains("\r\n", StringComparison.Ordinal) ? "\r\n" : "\n";
                text = Norm(decoded);
            }

            List<string> applied = [];
            foreach (var rewrite in catalog.CallRewrites)
            {
                // A file that does not mention the callee anywhere cannot
                // tokenize to it either, so skip the tokenizer. This pass
                // sweeps the whole engine tree for every rewrite; the substring
                // probe is much faster and exactly equivalent - a callee token
                // implies the substring.
                if (!text.Contains(rewrite.Callee, StringComparison.Ordinal)) continue;

                var sites = GmlScanner.FindCalls(text, rewrite.Callee);
                List<CallSite> direct = [];
                foreach (var site in sites)
                {
                    var line = CountLines(text, site.NameStart);
                    if (site.Kind == CallKind.Member)
                    {
                        problems.Add(new SeamProblem(
                            $"call_rewrite '{rewrite.Id}': {rel}:{line} reaches "
                            + $"'{rewrite.Callee}' through member access - the rewrite covers "
                            + "direct calls only; the engine changed; the seam catalog needs "
                            + "updating",
                            SeamProblemKind.CallRewrite, rewrite.Id, rel,
                            Line: line, Context: NumberedExcerpt(text, line)));
                        continue;
                    }

                    if (site.Kind == CallKind.Definition)
                    {
                        problems.Add(new SeamProblem(
                            $"call_rewrite '{rewrite.Id}': {rel}:{line} defines "
                            + $"'{rewrite.Callee}' - the engine grew a GML body for it; use a "
                            + "wrap seam instead",
                            SeamProblemKind.CallRewrite, rewrite.Id, rel,
                            Line: line, Context: NumberedExcerpt(text, line)));
                        continue;
                    }

                    if (site.Args != rewrite.Args)
                    {
                        problems.Add(new SeamProblem(
                            $"call_rewrite '{rewrite.Id}': {rel}:{line} passes {site.Args} "
                            + $"argument(s) to '{rewrite.Callee}' (expected {rewrite.Args}) - "
                            + "the engine changed the call shape; the seam catalog needs "
                            + "updating",
                            SeamProblemKind.CallRewrite, rewrite.Id, rel,
                            Line: line, Context: NumberedExcerpt(text, line)));
                    }

                    direct.Add(site);
                }

                if (direct.Count == 0) continue;
                for (var i = direct.Count - 1; i >= 0; i--)
                {
                    var site = direct[i];
                    text = text[..site.NameStart] + rewrite.To + text[site.NameEnd..];
                }

                siteCounts[rewrite.Id] += direct.Count;
                fileCounts[rewrite.Id]++;
                applied.Add(rewrite.Id);
            }

            if (applied.Count > 0)
            {
                if (current is null)
                {
                    current = new StagedFile(text, eol);
                    staged[rel] = current;
                }
                else
                {
                    current.Text = text;
                }

                current.AppliedIds.AddRange(applied);
            }
        }

        foreach (var rewrite in catalog.CallRewrites.Where(r => siteCounts[r.Id] == 0))
        {
            problems.Add(new SeamProblem(
                $"call_rewrite '{rewrite.Id}': no direct call to '{rewrite.Callee}' "
                + "anywhere in the engine tree - the engine renamed it; the seam catalog "
                + "needs updating",
                SeamProblemKind.CallRewrite, rewrite.Id));
        }

        if (problems.Count > priorProblems) return;

        foreach (var rewrite in catalog.CallRewrites)
        {
            Logger.Log($"  call rewrite '{rewrite.Id}': {rewrite.Callee} -> {rewrite.To}, "
                       + $"{siteCounts[rewrite.Id]} call site(s) in {fileCounts[rewrite.Id]} file(s)");
        }
    }

    // Apply a target-form seam: locate the function by name, then land the
    // payload structurally. Token matching, so indentation, blank-line and
    // comment drift in the engine file do not rot the locator. Fail-closed:
    // the function, and the anchor inside it, must each match exactly once.
    private static string ApplyTarget(SeamEntry entry, string text,
        IReadOnlySet<string>? siblingDeclared = null)
    {
        var tokens = GmlScanner.Tokenize(text);
        var spans = GmlScanner.FindFunctions(text, entry.TargetFn, tokens);
        if (spans.Count != 1)
        {
            throw new SeamProblemException(new SeamProblem(
                $"{entry.Kind.CatalogName()} '{entry.Id}': function '{entry.TargetFn}' defined "
                + $"{spans.Count}x in {entry.File} (expected 1) - the engine file changed; "
                + "the seam catalog needs updating",
                SeamProblemKind.Target, entry.Id, entry.File));
        }

        var span = spans[0];
        var fnLine = CountLines(text, span.BodyOpen);

        // Every scope read must occur in the target function's span, so a name
        // the engine renamed fails as loudly as an anchor miss.
        var missing = PayloadReads.ScopeReads(entry.Replace)
            .Where(name => !(siblingDeclared?.Contains(name) ?? false))
            .Where(name => !Mentions(text, tokens, span.Start, span.BodyClose, name))
            .ToList();
        if (missing.Count > 0)
        {
            var names = string.Join(", ", missing.Select(name => $"'{name}'"));
            throw new SeamProblemException(new SeamProblem(
                $"{entry.Kind.CatalogName()} '{entry.Id}': the payload reads {names} but "
                + $"'{entry.TargetFn}' in {entry.File} never mentions "
                + (missing.Count == 1 ? "it" : "them")
                + " - the engine file changed; the seam catalog needs updating",
                SeamProblemKind.Reads, entry.Id, entry.File,
                Line: fnLine, Context: NumberedExcerpt(text, fnLine)));
        }

        if (entry.Op == DispatchOp.Wrap) return ApplyWrap(entry, text, span, tokens);

        int pos;
        if (entry.TargetAt == "head")
        {
            // the payload lands on its own line after the opening brace, so
            // the brace must end its line (a one-line body would put the
            // payload outside the function)
            if (!RestOfLineIsBlank(text, span.BodyOpen + 1))
            {
                throw new SeamProblemException(new SeamProblem(
                    $"{entry.Kind.CatalogName()} '{entry.Id}': the body of '{entry.TargetFn}' opens and "
                    + $"continues on one line in {entry.File} - use a text seam",
                    SeamProblemKind.Target, entry.Id, entry.File,
                    Line: fnLine, Context: NumberedExcerpt(text, fnLine)));
            }

            pos = GmlScanner.NextLineStart(text, span.BodyOpen);
        }
        else
        {
            var matches = GmlScanner.FindAnchor(text, span.BodyOpen, span.BodyClose,
                entry.TargetAnchor, tokens);
            if (matches.Count != 1)
            {
                throw new SeamProblemException(new SeamProblem(
                    $"{entry.Kind.CatalogName()} '{entry.Id}': target anchor matched {matches.Count}x "
                    + $"inside '{entry.TargetFn}' in {entry.File} (expected 1) - the "
                    + "engine file changed; the seam catalog needs updating",
                    SeamProblemKind.Target, entry.Id, entry.File,
                    Line: fnLine, Context: NumberedExcerpt(text, fnLine)));
            }

            var (start, end) = matches[0];
            // the insertion is line-wise, so the match must own its lines:
            // code sharing the anchor's first or last line would end up on the
            // wrong side of the payload
            var prefix = text[GmlScanner.LineStart(text, start)..start];
            if (prefix.Trim().Length > 0 || !RestOfLineIsBlank(text, end))
            {
                var anchorLine = CountLines(text, start);
                throw new SeamProblemException(new SeamProblem(
                    $"{entry.Kind.CatalogName()} '{entry.Id}': the target anchor shares a line with other "
                    + $"code in {entry.File} - use a text seam",
                    SeamProblemKind.Target, entry.Id, entry.File,
                    Line: anchorLine, Context: NumberedExcerpt(text, anchorLine)));
            }

            pos = entry.TargetAt == "before"
                ? GmlScanner.LineStart(text, start)
                : GmlScanner.NextLineStart(text, end);
        }

        return text[..pos] + entry.Replace + text[pos..];
    }

    // Apply a wrap: rename the pristine definition to __mmapi_orig_<fn>, body
    // untouched, and emit the generated wrapper right after it. The body must
    // not call the function by its own name, or the rename would send those
    // calls through the wrapper and double-filter.
    private static string ApplyWrap(SeamEntry entry, string text, FunctionSpan span,
        List<GmlToken> tokens)
    {
        var name = entry.TargetFn;
        var selfCalls = GmlScanner.FindAnchor(text, span.BodyOpen, span.BodyClose, $"{name}(", tokens);
        if (selfCalls.Count > 0)
        {
            throw new SeamProblemException(new SeamProblem(
                $"{entry.Kind.CatalogName()} '{entry.Id}': the body of '{name}' calls '{name}' - "
                + "a wrap cannot rename a self-referencing function; use a text seam",
                SeamProblemKind.Wrap, entry.Id, entry.File));
        }

        var payload = entry.Replace;
        var blankBefore = payload.StartsWith('\n');
        var blankAfter = payload.EndsWith("\n\n", StringComparison.Ordinal);
        var filterLine = payload.Trim('\n');

        var wrapper = DispatchRenderer.RenderWrap(
            span.Form,
            name,
            span.Params,
            span.Args,
            GmlScanner.LineIndent(text, span.Start),
            filterLine,
            blankBefore,
            blankAfter);
        var renamed = text[..span.NameStart] + DispatchRenderer.OrigPrefix + name + text[span.NameEnd..];
        var insertAt = GmlScanner.NextLineStart(renamed, span.BodyClose + DispatchRenderer.OrigPrefix.Length);
        return renamed[..insertAt] + wrapper + renamed[insertAt..];
    }

    // The closest-match hint for a missed anchor. Whitespace drift is the
    // common rot, so check that first, then whether the anchor's first line
    // survives.
    private static string AnchorHint(string anchor, string text)
    {
        var squeezedAnchor = WhitespaceRuns.Replace(anchor, " ").Trim();
        if (WhitespaceRuns.Replace(text, " ").Contains(squeezedAnchor, StringComparison.Ordinal))
            return "the anchor matches when whitespace is collapsed - indentation or blank-line drift";

        var first = FirstLine(anchor);
        if (first.Length == 0) return "";
        var hits = CountOccurrences(text, first);
        if (hits == 1) return "the anchor's first line is present; the lines after it diverge";
        if (hits > 1) return $"the anchor's first line is present {hits}x; the lines after it diverge";
        return "no part of the anchor is present";
    }

    // A line-numbered pristine excerpt around a 1-based line, for re-anchoring
    // a missed seam without opening the file blind.
    private static string NumberedExcerpt(string text, int line, int before = 3, int after = 6)
    {
        var lines = text.Split('\n');
        var lo = Math.Max(0, line - 1 - before);
        var hi = Math.Min(lines.Length, line - 1 + after + 1);
        return string.Join("\n", Enumerable.Range(lo, hi - lo).Select(i => $"{i + 1,5}  {lines[i]}"));
    }

    // The best-guess pristine location for a missed anchor: the first anchor
    // line that still occurs in the file, in anchor order. (0, "") when no
    // line survives - the hint already says so.
    private static (int Line, string Context) ClosestContext(string anchor, string text)
    {
        foreach (var probe in anchor.Trim().Split('\n').Select(l => l.Trim()))
        {
            if (probe.Length == 0) continue;
            var pos = text.IndexOf(probe, StringComparison.Ordinal);
            if (pos == -1) continue;

            var line = CountLines(text, pos);
            return (line, NumberedExcerpt(text, line));
        }

        return (0, "");
    }

    // True when the identifier occurs as a whole token anywhere inside the
    // [start, end] region.
    private static bool Mentions(string text, List<GmlToken> tokens, int start, int end, string name)
    {
        foreach (var token in tokens)
        {
            if (token.Start < start) continue;
            if (token.Start > end) break;
            if (text.AsSpan(token.Start, token.End - token.Start).SequenceEqual(name)) return true;
        }

        return false;
    }

    // True when nothing but whitespace or a line comment follows pos on its line
    private static bool RestOfLineIsBlank(string text, int pos)
    {
        var lineEnd = text.IndexOf('\n', pos);
        var rest = (lineEnd == -1 ? text[pos..] : text[pos..lineEnd]).Trim();
        return rest.Length == 0 || rest.StartsWith("//", StringComparison.Ordinal);
    }

    // Non-overlapping occurrences, the anchor-count contract
    private static int CountOccurrences(string text, string value)
    {
        var count = 0;
        var index = 0;
        while ((index = text.IndexOf(value, index, StringComparison.Ordinal)) != -1)
        {
            count++;
            index += value.Length;
        }

        return count;
    }

    // 1-based line number of the char offset
    private static int CountLines(string text, int pos) =>
        text.AsSpan(0, pos).Count('\n') + 1;

    private static string FirstLine(string anchor)
    {
        var trimmed = anchor.Trim();
        var newline = trimmed.IndexOf('\n');
        return (newline == -1 ? trimmed : trimmed[..newline]).Trim();
    }

    private static string Norm(string text) => text.Replace("\r\n", "\n");
}
