using System.Text;
using Garethp.ModsOfMistriaInstallerLib.Seam;
using Garethp.ModsOfMistriaInstallerLib.Store;

namespace Garethp.ModsOfMistriaInstallerLib.Operations;

// One point's rebase outcome, listing every symbol with its old and new ordinal, in
// the order they were reassigned. Unchanged means the packing was already
// correct. Running the flag when nothing collided is a no-op, not a shuffle.
public record RebasedPoint(string PointId, int BaseLen,
    IReadOnlyList<(string Symbol, int OldOrdinal, int NewOrdinal)> Moves)
{
    public bool Changed => Moves.Any(m => m.OldOrdinal != m.NewOrdinal);
}

public class RebaseResult(IReadOnlyList<RebasedPoint> points, IReadOnlyList<SeamProblem> problems)
{
    public IReadOnlyList<RebasedPoint> Points { get; } = points;

    public IReadOnlyList<SeamProblem> Problems { get; } = problems;

    public bool Ok => Problems.Count == 0;

    public bool Changed => Points.Any(p => p.Changed);
}

// Repacks every point's assignments contiguously above a grown base enum's
// LEN, preserving relative order, symbols untouched. Save-invisible because
// the engine persists NPC state by name, so every install runs it
// automatically and logs each move.
public static class ExtensionRebaser
{
    // Compute the reassignment for every point, then apply it to the
    // in-memory ledger only when every point scanned clean. All or nothing:
    // a partial apply would persist silently-moved points beside a failed
    // one, and the caller's per-move log never runs on a failed result, so
    // the gate is what keeps the "every move is logged" contract true.
    // Nothing touches disk here. The caller decides whether to Save().
    public static RebaseResult Run(SeamCatalog catalog, IPristineSource pristine,
        ExtensionLedgerStore ledger)
    {
        List<RebasedPoint> points = [];
        List<SeamProblem> problems = [];
        List<(string PointId, List<ExtensionAssignment> Reassigned)> pending = [];

        foreach (var point in catalog.Extensions)
        {
            var assigned = ledger.Assignments(point.Id);
            if (assigned.Count == 0) continue;

            // Base LEN from the pristine enum. Pristine, not staged, because seams
            // never add enum members (only extensions do), so the pristine
            // count is the base count, and rebase must not depend on a full
            // stage succeeding, and staging is exactly what is broken when this
            // operation is the remedy.
            byte[]? raw;
            try
            {
                raw = pristine.Read(point.File);
            }
            catch (Exception exception)
            {
                problems.Add(new SeamProblem(
                    $"extension '{point.Id}': pristine {point.File} unreadable ({exception.Message})",
                    SeamProblemKind.Decode, $"ext:{point.Id}", point.File));
                continue;
            }

            if (raw is null)
            {
                problems.Add(new SeamProblem(
                    $"extension '{point.Id}': site file not found in pristine source: {point.File}",
                    SeamProblemKind.MissingFile, $"ext:{point.Id}", point.File));
                continue;
            }

            string text;
            try
            {
                text = StagingText.Norm(StagingText.Decode(raw));
            }
            catch (DecoderFallbackException exception)
            {
                problems.Add(new SeamProblem(
                    $"extension '{point.Id}': pristine {point.File} is not UTF-8 ({exception.Message})",
                    SeamProblemKind.Decode, $"ext:{point.Id}", point.File));
                continue;
            }

            var scan = ExtensionExpander.ScanOrdinalEnum(point, text, problems);
            if (scan is null) continue;

            var baseLen = scan.Members.Count - 1;

            // relative order preserved. Sort by old ordinal, pack from baseLen
            var next = baseLen;
            List<(string, int, int)> moves = [];
            List<ExtensionAssignment> reassigned = [];
            foreach (var assignment in assigned.OrderBy(a => a.Ordinal))
            {
                moves.Add((assignment.Symbol, assignment.Ordinal, next));
                reassigned.Add(assignment with { Ordinal = next });
                next++;
            }

            points.Add(new RebasedPoint(point.Id, baseLen, moves));
            pending.Add((point.Id, reassigned));
        }

        if (problems.Count == 0)
            foreach (var (pointId, reassigned) in pending)
                ledger.Rebase(pointId, reassigned);

        return new RebaseResult(points, problems);
    }
}
