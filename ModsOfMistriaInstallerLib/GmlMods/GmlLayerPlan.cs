using Garethp.ModsOfMistriaInstallerLib.Seam;

namespace Garethp.ModsOfMistriaInstallerLib.GmlMods;

// One excluded mod and every reason (one mod, one fate - a mod-content
// failure excludes the whole mod, content included, and the apply proceeds)
public class ExcludedMod(GmlModCode mod)
{
    public GmlModCode Mod { get; } = mod;

    public List<string> Reasons { get; } = [];
}

// The staged GML layer, validated in memory before a single byte lands in the
// store. The write phase copies Added and Seamed into the rebuilt archive.
public class GmlLayerPlan
{
    // rel → bytes: the mmapi framework, the surviving mods' gml, the hook catalog
    public Dictionary<string, byte[]> Added { get; } = [];

    public IReadOnlyDictionary<string, StagedFile> Seamed { get; internal set; } =
        new Dictionary<string, StagedFile>();

    public List<GmlModCode> Survivors { get; } = [];

    public List<ExcludedMod> Excluded { get; } = [];

    public List<LintFinding> Findings { get; } = [];

    // Ordinals the expansion assigned that the ledger does not yet hold. The
    // installer applies these only after the survivor set settles, and
    // persists them only alongside a successful commit, because an ordinal handed to
    // a mod that then fails to install would be burned forever.
    public List<ExtensionLedgerEntry> NewAssignments { get; } = [];
}
