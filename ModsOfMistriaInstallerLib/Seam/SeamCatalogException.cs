namespace Garethp.ModsOfMistriaInstallerLib.Seam;

// A malformed catalog. MOMI ships the catalog, so a schema violation, marker
// collision, lint failure or dependency cycle is a build defect: this is a bug
// report, not a user-facing state. Validation is batched - every problem
// across the whole catalog is collected before this throws.
public class SeamCatalogException(string sourceName, IReadOnlyList<string> problems)
    : Exception($"seam catalog {sourceName}: {problems.Count} problem(s):\n"
                + string.Join("\n", problems.Select(p => $"  - {p}")))
{
    public IReadOnlyList<string> Problems { get; } = problems;
}
