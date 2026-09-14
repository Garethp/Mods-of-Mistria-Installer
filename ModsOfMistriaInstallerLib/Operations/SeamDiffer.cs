using System.Globalization;
using System.Text;
using Garethp.ModsOfMistriaInstallerLib.Seam;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Garethp.ModsOfMistriaInstallerLib.Operations;

// The comparison verdict for one catalog entry's region between two builds.
public enum SeamRegionStatus
{
    Unchanged,
    Changed,
    OldMissing,
    NewMissing,
}

// One entry's region, compared between the old and new build. Region names the
// code that was compared. DiffLines is filled only when the region changed, and
// OldText and NewText ride along only when the changed region is small enough
// to quote whole.
public record SeamRegionDiff(
    string EntryId,
    string Kind,
    string File,
    string Region,
    SeamRegionStatus Status,
    string Note,
    IReadOnlyList<string> DiffLines,
    string OldText,
    string NewText,
    int RegionLines = 0,
    int RegionStartLine = 0);  // 1-based first line of the region in the new build

// The read-only triage result for one build pair. Ok only when every region is
// unchanged and neither archive carries catalog markers. ExitCode is the CLI
// contract, 0 when nothing needs review and 1 when anything does.
public class SeamDiffResult(IReadOnlyList<SeamRegionDiff> entries, IReadOnlyList<string> moddedMarkers)
{
    public IReadOnlyList<SeamRegionDiff> Entries { get; } = entries;

    // Catalog markers found in either archive. A side that carries one is a
    // modded build, and a diff against it is meaningless.
    public IReadOnlyList<string> ModdedMarkers { get; } = moddedMarkers;

    public int ChangedCount => Entries.Count(e => e.Status == SeamRegionStatus.Changed);

    public int MissingCount => Entries.Count(e =>
        e.Status is SeamRegionStatus.OldMissing or SeamRegionStatus.NewMissing);

    public bool Ok => ModdedMarkers.Count == 0 && ChangedCount == 0 && MissingCount == 0;

    public int ExitCode => Ok ? 0 : 1;
}

// Lists every catalog region that changed between two builds. The structural
// checks prove a seam still lands, and this shows whether the code around it
// changed while it kept landing. A changed region is review material, not a
// failure. The diff writes nothing, and its rendered output stays literal
// English like the seam check's.
public static class SeamDiffer
{
    // The context lines kept on each side of a top-level anchor, where no
    // enclosing function bounds the region.
    private const int WindowLines = 15;

    // The largest changed region whose full texts still ride in the result. A
    // larger region carries its hunked diff alone.
    private const int TextCarryLines = 120;

    private static readonly UTF8Encoding Utf8Strict = new(false, true);

    public static SeamDiffResult Diff(IPristineSource oldPristine, IPristineSource newPristine,
        SeamCatalog? catalog = null)
    {
        if (catalog is null)
        {
            var (name, bytes) = PayloadResolver.SeamCatalog();
            catalog = SeamCatalogLoader.Load(bytes, name);
        }

        Dictionary<string, string?> oldFiles = [];
        Dictionary<string, string?> newFiles = [];
        List<SeamRegionDiff> entries = [];
        List<string> moddedMarkers = [];

        foreach (var entry in catalog.Entries)
        {
            var oldText = LoadFile(oldPristine, entry.File, oldFiles);
            var newText = LoadFile(newPristine, entry.File, newFiles);

            if (oldText is not null && oldText.Contains(entry.Marker, StringComparison.Ordinal))
                moddedMarkers.Add($"'{entry.Marker}' ({entry.Id}) in the old {entry.File}");
            if (newText is not null && newText.Contains(entry.Marker, StringComparison.Ordinal))
                moddedMarkers.Add($"'{entry.Marker}' ({entry.Id}) in the new {entry.File}");

            var oldRegion = oldText is null
                ? new Region(null, "", "file not in the old archive")
                : ExtractRegion(entry, oldText);
            var newRegion = newText is null
                ? new Region(null, "", "file not in the new archive")
                : ExtractRegion(entry, newText);

            var kind = entry.Kind.CatalogName();
            if (newRegion.Text is null)
            {
                entries.Add(new SeamRegionDiff(entry.Id, kind, entry.File,
                    oldRegion.Description, SeamRegionStatus.NewMissing, newRegion.Note, [], "", ""));
                continue;
            }

            if (oldRegion.Text is null)
            {
                entries.Add(new SeamRegionDiff(entry.Id, kind, entry.File,
                    newRegion.Description, SeamRegionStatus.OldMissing, oldRegion.Note, [], "", ""));
                continue;
            }

            if (TokenForm(oldRegion.Text) == TokenForm(newRegion.Text))
            {
                entries.Add(new SeamRegionDiff(entry.Id, kind, entry.File,
                    newRegion.Description, SeamRegionStatus.Unchanged, "", [], "", ""));
                continue;
            }

            var regionLines = newRegion.Text.TrimEnd('\n').Split('\n').Length;
            var carryTexts = regionLines <= TextCarryLines
                             && oldRegion.Text.TrimEnd('\n').Split('\n').Length <= TextCarryLines;
            entries.Add(new SeamRegionDiff(entry.Id, kind, entry.File,
                newRegion.Description, SeamRegionStatus.Changed, "",
                DiffLines(oldRegion.Text, newRegion.Text, newRegion.StartLine),
                carryTexts ? oldRegion.Text : "",
                carryTexts ? newRegion.Text : "",
                regionLines, newRegion.StartLine));
        }

        return new SeamDiffResult(entries, moddedMarkers);
    }

    // The entry's region in one build. It is the target function's span, the
    // function enclosing the anchor widened to the anchor's extent, or a fixed
    // window of lines around a top-level anchor. Text is null when the locator
    // does not land, with the note saying why. StartLine is the 1-based file
    // line the region begins on.
    private record Region(string? Text, string Description, string Note, int StartLine = 0);

    private static Region ExtractRegion(SeamEntry entry, string text)
    {
        if (entry.TargetFn.Length > 0)
        {
            var spans = GmlScanner.FindFunctions(text, entry.TargetFn);
            var description = $"function '{entry.TargetFn}'";
            if (spans.Count != 1)
                return new Region(null, description, $"function '{entry.TargetFn}' defined {spans.Count}x");
            var span = spans[0];
            return new Region(text[span.Start..(span.BodyClose + 1)], description, "",
                LineOf(text, span.Start));
        }

        var occurrences = CountOccurrences(text, entry.Anchor);
        if (occurrences != 1)
            return new Region(null, "the lines around the anchor", $"anchor matched {occurrences}x");

        var matchStart = text.IndexOf(entry.Anchor, StringComparison.Ordinal);
        var matchEnd = matchStart + entry.Anchor.Length;
        var enclosing = GmlScanner.EnclosingFunction(text, matchStart);
        if (enclosing is not null)
        {
            var start = Math.Min(enclosing.Start, matchStart);
            var end = Math.Max(enclosing.BodyClose + 1, matchEnd);
            return new Region(text[start..end], $"function '{enclosing.Name}' around the anchor", "",
                LineOf(text, start));
        }

        var windowStart = WalkLines(text, matchStart, -WindowLines);
        return new Region(text[windowStart..WalkLines(text, matchEnd, WindowLines)],
            "the lines around the anchor", "", LineOf(text, windowStart));
    }

    // The 1-based line number of the char offset.
    private static int LineOf(string text, int pos) =>
        text.AsSpan(0, pos).Count('\n') + 1;

    // The char offset `count` line starts away from pos, backwards to a line
    // start or forwards past that many line ends, clamped to the text.
    private static int WalkLines(string text, int pos, int count)
    {
        if (count < 0)
        {
            var at = GmlScanner.LineStart(text, pos);
            for (var i = 0; i > count && at > 0; i--) at = GmlScanner.LineStart(text, at - 1);
            return at;
        }

        var offset = pos;
        for (var i = 0; i < count && offset < text.Length; i++)
            offset = GmlScanner.NextLineStart(text, offset);
        return offset;
    }

    // Whitespace- and comment-insensitive comparison form, the same token
    // discipline the stager matches anchors with.
    private static string TokenForm(string region) =>
        string.Join(" ", GmlScanner.Tokenize(region).Select(t => region[t.Start..t.End]));

    // A hunked line diff, with old-only lines prefixed "- ", new-only lines
    // prefixed "+ ", and common context prefixed "  ". Only the changed lines
    // and their neighborhood print, and every elided stretch states its size
    // and the new-build line it resumes at. An elided stretch holds only
    // unchanged lines, because changed lines are always kept.
    private static List<string> DiffLines(string oldText, string newText, int newStartLine)
    {
        var marked = MarkedLines(oldText, newText, newStartLine);
        var keep = new bool[marked.Count];
        for (var i = 0; i < marked.Count; i++)
        {
            if (marked[i].Line.StartsWith("  ", StringComparison.Ordinal)) continue;
            for (var j = Math.Max(0, i - 3); j <= Math.Min(marked.Count - 1, i + 3); j++)
                keep[j] = true;
        }

        List<string> lines = [];
        var unchanged = 0;
        for (var i = 0; i < marked.Count; i++)
        {
            if (!keep[i])
            {
                unchanged++;
                continue;
            }

            if (unchanged > 0)
            {
                lines.Add($"  ... ({Count(unchanged)} lines unchanged, resuming at {marked[i].NewLine})");
                unchanged = 0;
            }

            lines.Add(marked[i].Line);
        }

        if (unchanged > 0) lines.Add($"  ... ({Count(unchanged)} lines unchanged)");
        return lines;
    }

    private static string Count(int value) => value.ToString("N0", CultureInfo.InvariantCulture);

    // The full marked listing the hunks are cut from, each line carrying the
    // new-build file line it sits at. An old-only line carries the line the
    // new build resumes at after it.
    private static List<(string Line, int NewLine)> MarkedLines(string oldText, string newText,
        int newStartLine)
    {
        var a = oldText.TrimEnd('\n').Split('\n');
        var b = newText.TrimEnd('\n').Split('\n');
        var lcs = new int[a.Length + 1, b.Length + 1];
        for (var i = a.Length - 1; i >= 0; i--)
        for (var j = b.Length - 1; j >= 0; j--)
            lcs[i, j] = a[i] == b[j] ? lcs[i + 1, j + 1] + 1 : Math.Max(lcs[i + 1, j], lcs[i, j + 1]);

        List<(string Line, int NewLine)> lines = [];
        var x = 0;
        var y = 0;
        while (x < a.Length && y < b.Length)
        {
            if (a[x] == b[y])
            {
                lines.Add(("  " + a[x], newStartLine + y));
                x++;
                y++;
            }
            else if (lcs[x + 1, y] >= lcs[x, y + 1])
            {
                lines.Add(("- " + a[x], newStartLine + y));
                x++;
            }
            else
            {
                lines.Add(("+ " + b[y], newStartLine + y));
                y++;
            }
        }

        while (x < a.Length) lines.Add(("- " + a[x++], newStartLine + y));
        while (y < b.Length)
        {
            lines.Add(("+ " + b[y], newStartLine + y));
            y++;
        }

        return lines;
    }

    private static string? LoadFile(IPristineSource pristine, string rel, Dictionary<string, string?> cache)
    {
        if (cache.TryGetValue(rel, out var cached)) return cached;

        string? text = null;
        var raw = pristine.Read(rel);
        if (raw is not null)
        {
            try
            {
                text = Utf8Strict.GetString(raw).Replace("\r\n", "\n");
            }
            catch (DecoderFallbackException)
            {
                text = null;
            }
        }

        cache[rel] = text;
        return text;
    }

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

    // The human-readable report. Only regions needing attention print. A
    // changed region means the seam and its hook contract need review against
    // the new code, not that staging fails.
    public static string RenderText(SeamDiffResult result, string oldSource, string newSource)
    {
        List<string> lines =
        [
            $"seam-diff {oldSource} -> {newSource}",
            $"  catalog: {result.Entries.Count} regions compared",
        ];

        if (result.ModdedMarkers.Count > 0)
        {
            lines.Add("  RESULT: REFUSED - catalog markers found, so a compared build is modded, not pristine:");
            lines.AddRange(result.ModdedMarkers.Select(m => $"  - {m}"));
            lines.Add("  Uninstall first, or point the diff at pristine archives.");
            return string.Join("\n", lines);
        }

        if (result.Ok)
        {
            lines.Add("  RESULT: OK - no seam region changed between these builds");
            return string.Join("\n", lines);
        }

        lines.Add($"  RESULT: REVIEW - {result.ChangedCount} region(s) changed, {result.MissingCount} missing");
        foreach (var entry in result.Entries.Where(e => e.Status != SeamRegionStatus.Unchanged))
        {
            var status = entry.Status switch
            {
                SeamRegionStatus.Changed => "CHANGED",
                SeamRegionStatus.OldMissing => $"OLD MISSING - {entry.Note}",
                _ => $"NEW MISSING - {entry.Note}",
            };
            var where = entry.RegionStartLine > 0
                ? $"{entry.Region} in {entry.File} "
                  + $"(lines {entry.RegionStartLine}-{entry.RegionStartLine + entry.RegionLines - 1})"
                : $"{entry.Region} in {entry.File}";
            lines.Add("");
            lines.Add($"  - {entry.Kind} '{entry.EntryId}': {where}: {status}");
            lines.AddRange(entry.DiffLines.Select(l => "    " + l));
        }

        lines.Add("");
        lines.Add("  Changed regions still stage; review each seam's promises and its hook's documented");
        lines.Add("  contract against the new code. Run --seam-check for the fail-closed verdict.");
        return string.Join("\n", lines);
    }

    // The machine-readable report. Every entry appears with its status, and the
    // texts and diff ride along only for changed regions.
    public static string ToJson(SeamDiffResult result, string oldSource, string newSource) => new JObject
    {
        ["ok"] = result.Ok,
        ["old"] = oldSource,
        ["new"] = newSource,
        ["regions"] = result.Entries.Count,
        ["changed"] = result.ChangedCount,
        ["missing"] = result.MissingCount,
        ["modded_markers"] = new JArray(result.ModdedMarkers),
        ["entries"] = new JArray(result.Entries.Select(e => new JObject
        {
            ["id"] = e.EntryId,
            ["kind"] = e.Kind,
            ["file"] = e.File,
            ["region"] = e.Region,
            ["status"] = e.Status switch
            {
                SeamRegionStatus.Unchanged => "unchanged",
                SeamRegionStatus.Changed => "changed",
                SeamRegionStatus.OldMissing => "old_missing",
                _ => "new_missing",
            },
            ["note"] = e.Note,
            ["region_start_line"] = e.RegionStartLine,
            ["region_lines"] = e.RegionLines,
            ["old_text"] = e.OldText,
            ["new_text"] = e.NewText,
            ["diff"] = new JArray(e.DiffLines),
        })),
    }.ToString(Formatting.Indented);
}
