using Garethp.ModsOfMistriaInstallerLib.Seam;

namespace ModsOfMistriaInstallerLibTests.TestUtils;

// Synthesises a pristine stand-in from a catalog's own anchors:
// containment-deduped text anchors joined by blank lines, plus function
// skeletons carrying the token anchors of targeted functions. This is what
// proves the shipped catalog against itself with no game files.
public static class PristineSynthesis
{
    public static Dictionary<string, string> FromCatalog(SeamCatalog catalog)
    {
        List<string> fileOrder = [];
        Dictionary<string, List<SeamEntry>> byFile = [];
        foreach (var entry in catalog.Entries)
        {
            if (!byFile.TryGetValue(entry.File, out var group))
            {
                group = [];
                byFile[entry.File] = group;
                fileOrder.Add(entry.File);
            }

            group.Add(entry);
        }

        Dictionary<string, string> pristine = [];
        foreach (var file in fileOrder)
        {
            var entries = byFile[file];
            var anchors = entries
                .Where(e => e.Anchor.Length > 0)
                .Select(e => e.Anchor)
                .ToList();
            List<string> survivors = [];
            foreach (var anchor in anchors)
            {
                // contained in a larger anchor (stacked-seam case)
                if (anchors.Any(other => anchor != other && other.Contains(anchor, StringComparison.Ordinal)))
                    continue;
                if (!survivors.Contains(anchor)) survivors.Add(anchor);
            }

            var text = survivors.Count > 0 ? string.Join("\n\n", survivors) + "\n" : "";

            // target and wrap seams resolve against a function body, so
            // synthesise a skeleton for any targeted function the text anchors
            // do not already carry, its body holding that function's token anchors
            List<string> fnOrder = [];
            Dictionary<string, List<SeamEntry>> targeted = [];
            foreach (var entry in entries.Where(e => e.TargetFn.Length > 0))
            {
                if (!targeted.TryGetValue(entry.TargetFn, out var group))
                {
                    group = [];
                    targeted[entry.TargetFn] = group;
                    fnOrder.Add(entry.TargetFn);
                }

                group.Add(entry);
            }

            foreach (var fn in fnOrder)
            {
                if (GmlScanner.FindFunctions(text, fn).Count > 0) continue;
                var body = string.Concat(targeted[fn]
                    .Where(e => e.TargetAnchor.Length > 0)
                    .Select(e => $"    {e.TargetAnchor}\n"));
                text += (text.Length > 0 ? "\n" : "") + $"function {fn}() {{\n{body}}}\n";
            }

            pristine[file] = text;
        }

        return pristine;
    }
}
