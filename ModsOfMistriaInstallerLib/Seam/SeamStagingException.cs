namespace Garethp.ModsOfMistriaInstallerLib.Seam;

// Anchors that no longer land: a stale anchor, a missing pristine entry, a
// non-UTF-8 file or call-rewrite drift means the game build moved under a
// catalog that was correct when it shipped. Batched: every problem across the
// whole catalog is collected before this throws.
public class SeamStagingException(string summary, IReadOnlyList<SeamProblem> problems)
    : Exception($"{summary}, {problems.Count} problem(s):\n"
                + string.Join("\n", problems.Select(p => $"  - {p.Message}")))
{
    public IReadOnlyList<SeamProblem> Problems { get; } = problems;
}
