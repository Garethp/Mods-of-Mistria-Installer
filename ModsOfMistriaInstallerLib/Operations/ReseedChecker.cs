using Garethp.ModsOfMistriaInstallerLib.Seam;
using Garethp.ModsOfMistriaInstallerLib.Store;

namespace Garethp.ModsOfMistriaInstallerLib.Operations;

// One extension point's view of the reseed: what the saves name, what the
// outgoing archive's markers name, what the union would recover, and what
// the ledger already holds.
public sealed record ReseedPointReport(
    string PointId,
    int BaseLen,
    IReadOnlyList<string> FromSaves,
    IReadOnlyList<string> FromArchive,
    IReadOnlyList<string> WouldRecover,
    IReadOnlyList<string> AlreadyInLedger);

// The read-only reseed report. Notes are every message the harvesters would
// have logged on an install, and any note means a source did not read as
// expected, which is the one outcome the reseed must never leave silent.
public sealed class ReseedCheckResult(IReadOnlyList<ReseedPointReport> points, IReadOnlyList<string> notes,
    int saveCount, bool archiveChecked, bool ledgerChecked)
{
    public IReadOnlyList<ReseedPointReport> Points { get; } = points;

    public IReadOnlyList<string> Notes { get; } = notes;

    public int SaveCount { get; } = saveCount;

    public bool ArchiveChecked { get; } = archiveChecked;

    public bool LedgerChecked { get; } = ledgerChecked;

    public bool Ok => Notes.Count == 0;

    public int ExitCode => Ok ? 0 : 1;
}

// What the ledger reseed would recover, without installing. Runs the same
// two harvests the install runs, over the same saves folder and pristine
// archive, and reports instead of assigning. Nothing is written.
public static class ReseedChecker
{
    public static ReseedCheckResult Check(string savesDir, IPristineSource pristine,
        string? liveArchivePath = null, IExtensionLedger? ledger = null, SeamCatalog? catalog = null)
    {
        if (catalog is null)
        {
            var (name, bytes) = PayloadResolver.SeamCatalog();
            catalog = SeamCatalogLoader.Load(bytes, name);
        }

        List<string> notes = [];
        var harvest = SaveSymbolHarvester.Harvest(savesDir, catalog, pristine, notes);
        var fromSaves = harvest.ToDictionary(h => h.Key, h => Sorted(h.Value.Symbols), StringComparer.Ordinal);

        var fromArchive = new Dictionary<string, HashSet<string>>(StringComparer.Ordinal);
        if (liveArchivePath is not null)
        {
            fromArchive = ArchiveMarkerHarvester.Harvest(liveArchivePath, catalog, notes);
            SaveSymbolHarvester.UnionArchiveMarkers(harvest, fromArchive, notes);
        }

        List<ReseedPointReport> points = [];
        foreach (var point in catalog.Extensions)
        {
            if (!harvest.TryGetValue(point.Id, out var found)) continue;

            var known = ledger?.Assignments(point.Id).Select(a => a.Symbol).ToHashSet(StringComparer.Ordinal)
                        ?? new HashSet<string>(StringComparer.Ordinal);
            var union = Sorted(found.Symbols);
            points.Add(new ReseedPointReport(
                point.Id,
                found.BaseLen,
                fromSaves[point.Id],
                fromArchive.TryGetValue(point.Id, out var markers) ? Sorted(markers) : [],
                union.Where(s => !known.Contains(s)).ToList(),
                union.Where(known.Contains).ToList()));
        }

        var saveCount = 0;
        try
        {
            saveCount = Directory.EnumerateFiles(savesDir, "game-*.sav").Count();
        }
        catch (Exception)
        {
            // already a note from the harvest
        }

        return new ReseedCheckResult(points, notes, saveCount, liveArchivePath is not null, ledger is not null);
    }

    public static string RenderText(ReseedCheckResult result, string savesDir, string pristineSource,
        string? liveArchivePath)
    {
        List<string> lines =
        [
            $"reseed-check {savesDir}",
            $"  pristine: {pristineSource}; saves: {result.SaveCount}; outgoing archive: "
            + (result.ArchiveChecked ? liveArchivePath : "none, marker half skipped")
            + "; ledger: " + (result.LedgerChecked ? "read" : "none, nothing subtracted"),
        ];

        foreach (var point in result.Points)
        {
            lines.Add($"  '{point.PointId}': base {point.BaseLen}, from saves {Count(point.FromSaves)}, "
                      + $"from archive {Count(point.FromArchive)}, would recover {Count(point.WouldRecover)}, "
                      + $"already in ledger {Count(point.AlreadyInLedger)}");
        }

        if (result.Notes.Count > 0)
        {
            lines.Add("  notes:");
            lines.AddRange(result.Notes.Select(note => $"    - {note}"));
        }

        lines.Add(result.Ok
            ? "  RESULT: OK - every save and the outgoing archive read as expected"
            : "  RESULT: ATTENTION - a source did not read as expected, see the notes above");
        return string.Join("\n", lines);
    }

    private static List<string> Sorted(IEnumerable<string> names) =>
        names.OrderBy(n => n, StringComparer.Ordinal).ToList();

    private static string Count(IReadOnlyList<string> names) =>
        names.Count == 0 ? "0" : $"{names.Count} ({string.Join(", ", names)})";
}
