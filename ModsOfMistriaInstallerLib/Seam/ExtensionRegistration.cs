namespace Garethp.ModsOfMistriaInstallerLib.Seam;

// One mod's registration against one extension point. RenderedValues holds
// the already-validated, already-escaped GML spellings, so the expander is
// pure string assembly and never sees raw mod input.
public record ExtensionRegistration(
    string PointId,
    string Symbol,        // {mod.Symbol}_{local_name}, collision-free by construction
    string LocalName,
    string ModId,
    IReadOnlyDictionary<string, string> RenderedValues);

// One symbol's permanent ordinal assignment. Append-only. The entry survives
// the mod being uninstalled, since a save naming a removed member would
// otherwise throw in the engine's native string_to_X converter.
public record ExtensionAssignment(string Symbol, int Ordinal, string ModId);

// One ordinal the expander wants recorded, not yet recorded. The caller
// applies these once the survivor set has settled and persists them only
// alongside a successful commit, so a failed install never burns an ordinal.
public record ExtensionLedgerEntry(string PointId, ExtensionAssignment Assignment);

// What one expansion produced, beyond its in-place edits to the staged text.
public class ExtensionExpansion
{
    // Maps rel to bytes, holding the per-vacancy data stubs and the generated registry.
    public Dictionary<string, byte[]> Added { get; } = [];

    // The ordinals this run assigned, in assignment order, for the caller to
    // commit once it knows the mods holding them survived.
    public List<ExtensionLedgerEntry> NewAssignments { get; } = [];
}

// The per-game-install ordinal ledger. The expander takes this interface so
// --seam-check and the tests can drive it from memory.
public interface IExtensionLedger
{
    // Every recorded assignment for a point, in no guaranteed order. Empty
    // for an unknown point.
    IReadOnlyList<ExtensionAssignment> Assignments(string pointId);

    // Record a newly assigned ordinal. Persistence happens only alongside a
    // successful commit.
    void Assign(string pointId, ExtensionAssignment assignment);
}

// The in-memory ledger. --seam-check validates against it with nothing
// assigned, so every point is checked zero-registrant, and the expander tests
// drive it.
public class MemoryExtensionLedger : IExtensionLedger
{
    private readonly Dictionary<string, List<ExtensionAssignment>> _points = [];

    public MemoryExtensionLedger(params (string PointId, ExtensionAssignment Assignment)[] seed)
    {
        foreach (var (pointId, assignment) in seed) Assign(pointId, assignment);
    }

    public IReadOnlyList<ExtensionAssignment> Assignments(string pointId) =>
        _points.TryGetValue(pointId, out var assignments) ? assignments : [];

    public void Assign(string pointId, ExtensionAssignment assignment)
    {
        if (!_points.TryGetValue(pointId, out var assignments))
        {
            assignments = [];
            _points[pointId] = assignments;
        }

        assignments.Add(assignment);
    }
}
