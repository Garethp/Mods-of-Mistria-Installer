using System.Text;
using System.Text.RegularExpressions;

namespace Garethp.ModsOfMistriaInstallerLib.Seam;

// The text mechanics both staging layers share, meaning pristine decode, EOL
// normalisation, occurrence counting, line ownership, and the closest-match
// diagnostics a missed anchor reports. Extracted from SeamStager when the
// extension expander needed the same rules, because anchor discipline forked in two
// places is anchor discipline that drifts.
public static class StagingText
{
    private static readonly Regex WhitespaceRuns = new(@"\s+");

    private static readonly UTF8Encoding Utf8Strict = new(false, true);

    // Strict UTF-8. Throws DecoderFallbackException. Each caller turns that
    // into its own problem record, because the wording names the entry.
    public static string Decode(byte[] raw) => Utf8Strict.GetString(raw);

    public static string Norm(string text) => text.Replace("\r\n", "\n");

    public static string DetectEol(string text) =>
        text.Contains("\r\n", StringComparison.Ordinal) ? "\r\n" : "\n";

    // Pristine bytes to a staged file, decoded, EOL detected, and \n-normalised.
    public static StagedFile Load(byte[] raw)
    {
        var text = Decode(raw);
        return new StagedFile(Norm(text), DetectEol(text));
    }

    // Non-overlapping occurrences, the anchor-count contract
    public static int CountOccurrences(string text, string value)
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
    public static int CountLines(string text, int pos) =>
        text.AsSpan(0, pos).Count('\n') + 1;

    // True when nothing but whitespace or a line comment follows pos on its line
    public static bool RestOfLineIsBlank(string text, int pos)
    {
        var lineEnd = text.IndexOf('\n', pos);
        var rest = (lineEnd == -1 ? text[pos..] : text[pos..lineEnd]).Trim();
        return rest.Length == 0 || rest.StartsWith("//", StringComparison.Ordinal);
    }

    // True when the [start, end) span owns every line it covers. A line-wise
    // insertion beside a span that shares a line with other code would put that
    // code on the wrong side of the payload.
    public static bool OwnsItsLines(string text, int start, int end)
    {
        var prefix = text[LineStartOf(text, start)..start];
        return prefix.Trim().Length == 0 && RestOfLineIsBlank(text, end);
    }

    // A line-numbered excerpt around a 1-based line, for re-anchoring a missed
    // anchor without opening the file blind.
    public static string NumberedExcerpt(string text, int line, int before = 3, int after = 6)
    {
        var lines = text.Split('\n');
        var lo = Math.Max(0, line - 1 - before);
        var hi = Math.Min(lines.Length, line - 1 + after + 1);
        return string.Join("\n", Enumerable.Range(lo, hi - lo).Select(i => $"{i + 1,5}  {lines[i]}"));
    }

    // The closest-match hint for a missed anchor. Whitespace drift is the
    // common rot, so check that first, then whether the anchor's first line
    // survives.
    public static string AnchorHint(string anchor, string text)
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

    // The best-guess location for a missed anchor, the first anchor line that
    // still occurs in the file, in anchor order. (0, "") when no line survives -
    // the hint already says so.
    public static (int Line, string Context) ClosestContext(string anchor, string text)
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

    private static string FirstLine(string anchor)
    {
        var trimmed = anchor.Trim();
        var newline = trimmed.IndexOf('\n');
        return (newline == -1 ? trimmed : trimmed[..newline]).Trim();
    }

    private static int LineStartOf(string text, int pos) =>
        pos <= 0 ? 0 : text.LastIndexOf('\n', pos - 1) + 1;
}
