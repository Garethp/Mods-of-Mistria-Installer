using System.Text;
using Garethp.ModsOfMistriaInstallerLib.Operations;
using Garethp.ModsOfMistriaInstallerLib.Seam;
using Garethp.ModsOfMistriaInstallerLib.Store;

namespace ModsOfMistriaInstallerLibTests.Operations;

// The rebase recovery. Ordinal moves must preserve relative order and never touch
// the symbol set. The tombstone guarantee is about names, not numbers.
[TestFixture]
public class ExtensionRebaserTest
{
    private const string Catalog = """
        version = 2

        [[extension]]
        id   = "roster"
        file = "gml/NpcId.gml"

        [extension.ordinal]
        enum     = "NpcId"
        sentinel = "LEN"

        [[extension.sites]]
        id       = "enum_member"
        kind     = "enum_member"
        template = "{{symbol}} = {{ordinal}},"
        indent   = 4

        [extension.vacancy]
        enum_member = "{{symbol}} = {{ordinal}},"
        """ + "\n";

    private string _root = "";

    [SetUp]
    public void CreateTempDir()
    {
        _root = Path.Combine(Path.GetTempPath(), "momi_rebase_" + Path.GetRandomFileName());
        Directory.CreateDirectory(_root);
    }

    [TearDown]
    public void RemoveTempDir() => Directory.Delete(_root, true);

    private static SeamCatalog LoadCatalog() =>
        SeamCatalogLoader.Load(Encoding.UTF8.GetBytes(Catalog), "seams.toml");

    // an enum with `count` base members plus LEN
    private static MemoryPristineSource Pristine(int count)
    {
        var members = string.Concat(Enumerable.Range(0, count).Select(i => $"    M{i},\n"));
        return new MemoryPristineSource(new Dictionary<string, byte[]>
        {
            ["assets/gml/NpcId.gml"] = Encoding.UTF8.GetBytes($"enum NpcId {{\n{members}    LEN\n}}\n"),
        });
    }

    private ExtensionLedgerStore Ledger(params (string Symbol, int Ordinal)[] assigned)
    {
        var ledger = ExtensionLedgerStore.Load(_root);
        foreach (var (symbol, ordinal) in assigned)
            ledger.Assign("roster", new ExtensionAssignment(symbol, ordinal, "mod.x"));
        ledger.Save();
        return ExtensionLedgerStore.Load(_root);
    }

    [Test]
    public void ShouldPackAssignmentsAboveTheGrownBase()
    {
        // the game grew 2 -> 4 base members, colliding with both assignments
        var ledger = Ledger(("mod_luna", 2), ("mod_wren", 3));

        var result = ExtensionRebaser.Run(LoadCatalog(), Pristine(4), ledger);

        Assert.That(result.Ok, Is.True);
        Assert.That(result.Changed, Is.True);
        Assert.That(ledger.Assignments("roster").Select(a => (a.Symbol, a.Ordinal)), Is.EqualTo(new[]
        {
            ("mod_luna", 4),
            ("mod_wren", 5),
        }));
    }

    [Test]
    public void ShouldPreserveRelativeOrderIncludingVacancies()
    {
        // mod_gone is a tombstone (no live mod). It moves like everything else
        // and keeps its place in line. A rebase never drops a symbol
        var ledger = Ledger(("mod_gone", 2), ("mod_luna", 3), ("mod_wren", 4));

        var result = ExtensionRebaser.Run(LoadCatalog(), Pristine(5), ledger);

        Assert.That(result.Ok, Is.True);
        Assert.That(ledger.Assignments("roster").Select(a => (a.Symbol, a.Ordinal)), Is.EqualTo(new[]
        {
            ("mod_gone", 5),
            ("mod_luna", 6),
            ("mod_wren", 7),
        }));
    }

    [Test]
    public void ShouldBeANoOpWhenNothingCollides()
    {
        var ledger = Ledger(("mod_luna", 2), ("mod_wren", 3));

        var result = ExtensionRebaser.Run(LoadCatalog(), Pristine(2), ledger);

        Assert.That(result.Ok, Is.True);
        Assert.That(result.Changed, Is.False);
        Assert.That(ledger.Assignments("roster").Select(a => a.Ordinal), Is.EqualTo(new[] { 2, 3 }));
        Assert.That(ledger.Dirty, Is.False,
            "a rebase that moves nothing must not rewrite the ledger file");
    }

    private const string TwoPointCatalog = """
        version = 2

        [[extension]]
        id   = "roster"
        file = "gml/NpcId.gml"

        [extension.ordinal]
        enum     = "NpcId"
        sentinel = "LEN"

        [[extension.sites]]
        id       = "enum_member"
        kind     = "enum_member"
        template = "{{symbol}} = {{ordinal}},"
        indent   = 4

        [extension.vacancy]
        enum_member = "{{symbol}} = {{ordinal}},"

        [[extension]]
        id   = "status"
        file = "gml/StatusId.gml"

        [extension.ordinal]
        enum     = "StatusId"
        sentinel = "LEN"

        [[extension.sites]]
        id       = "enum_member"
        kind     = "enum_member"
        template = "{{symbol}} = {{ordinal}},"
        indent   = 4

        [extension.vacancy]
        enum_member = "{{symbol}} = {{ordinal}},"
        """ + "\n";

    [Test]
    public void ShouldApplyNothingWhenAnyPointFailsToScan()
    {
        // All or nothing across points. Applying the surviving point beside
        // a failed one would persist silently-moved ordinals, because the
        // caller's per-move log never runs on a failed result.
        var ledger = ExtensionLedgerStore.Load(_root);
        ledger.Assign("roster", new ExtensionAssignment("mod_luna", 2, "mod.x"));
        ledger.Assign("status", new ExtensionAssignment("mod_haste", 1, "mod.x"));
        ledger.Save();
        ledger = ExtensionLedgerStore.Load(_root);

        var catalog = SeamCatalogLoader.Load(Encoding.UTF8.GetBytes(TwoPointCatalog), "seams.toml");
        var pristine = new MemoryPristineSource(new Dictionary<string, byte[]>
        {
            // roster grew (a real move is pending), status is unscannable
            ["assets/gml/NpcId.gml"] = Encoding.UTF8.GetBytes(
                "enum NpcId {\n    M0,\n    M1,\n    M2,\n    LEN\n}\n"),
            ["assets/gml/StatusId.gml"] = Encoding.UTF8.GetBytes("enum Renamed {\n    A,\n    LEN\n}\n"),
        });

        var result = ExtensionRebaser.Run(catalog, pristine, ledger);

        Assert.That(result.Ok, Is.False);
        Assert.That(ledger.Assignments("roster").Single().Ordinal, Is.EqualTo(2),
            "the healthy point must not move while its sibling failed");
        Assert.That(ledger.Assignments("status").Single().Ordinal, Is.EqualTo(1));
        Assert.That(ledger.Dirty, Is.False);
    }

    [Test]
    public void ShouldCloseALedgerHoleWhilePreservingOrder()
    {
        // a hole (2, 5) is the launch-crash state. Rebase legitimately repairs
        // it because packing is contiguous by construction
        var ledger = Ledger(("mod_luna", 2), ("mod_wren", 5));

        var result = ExtensionRebaser.Run(LoadCatalog(), Pristine(2), ledger);

        Assert.That(result.Changed, Is.True);
        Assert.That(ledger.Assignments("roster").Select(a => (a.Symbol, a.Ordinal)), Is.EqualTo(new[]
        {
            ("mod_luna", 2),
            ("mod_wren", 3),
        }));
    }

    [Test]
    public void ShouldReportAndLeaveTheLedgerAloneOnAScanFailure()
    {
        var ledger = Ledger(("mod_luna", 2));
        var pristine = new MemoryPristineSource(new Dictionary<string, byte[]>
        {
            ["assets/gml/NpcId.gml"] = Encoding.UTF8.GetBytes("enum Renamed {\n    A,\n    LEN\n}\n"),
        });

        var result = ExtensionRebaser.Run(LoadCatalog(), pristine, ledger);

        Assert.That(result.Ok, Is.False);
        Assert.That(result.Problems.Single().Kind, Is.EqualTo(SeamProblemKind.Target));
        Assert.That(ledger.Dirty, Is.False, "a failed scan must not dirty the ledger");
    }

    [Test]
    public void ShouldSkipPointsWithNoAssignments()
    {
        var ledger = ExtensionLedgerStore.Load(_root);

        var result = ExtensionRebaser.Run(LoadCatalog(), Pristine(2), ledger);

        Assert.That(result.Ok, Is.True);
        Assert.That(result.Points, Is.Empty);
    }

    [Test]
    public void ShouldRefuseARebaseThatChangesTheSymbolSet()
    {
        var ledger = Ledger(("mod_luna", 2));

        Assert.Throws<InvalidOperationException>(() =>
            ledger.Rebase("roster", [new ExtensionAssignment("mod_other", 2, "mod.x")]));
    }

    [Test]
    public void ShouldRecordEachMoveWithOldAndNewOrdinals()
    {
        // The install log names each move from these recorded pairs, which
        // is the whole user-facing surface now that the manual flag is gone.
        var ledger = Ledger(("mod_luna", 2));

        var result = ExtensionRebaser.Run(LoadCatalog(), Pristine(3), ledger);

        Assert.That(result.Points.Single().Moves, Is.EqualTo(new[] { ("mod_luna", 2, 3) }));
    }
}
