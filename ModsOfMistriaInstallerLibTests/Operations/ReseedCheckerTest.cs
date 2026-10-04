using System.IO.Compression;
using System.Text;
using Garethp.ModsOfMistriaInstallerLib.Operations;
using Garethp.ModsOfMistriaInstallerLib.Seam;
using ModsOfMistriaInstallerLibTests.TestUtils;

namespace ModsOfMistriaInstallerLibTests.Operations;

// The read-only reseed check over synthetic saves and a synthetic outgoing
// archive: what it reports, what it subtracts, and that every harvester
// note reaches the result.
[TestFixture]
public class ReseedCheckerTest
{
    private const string Catalog = """
        version = 2

        [[extension]]
        id   = "npc_roster"
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

    private const string NpcEnum = "enum NpcId {\n    Ari,\n    Eiland,\n    LEN,\n}\n";

    private static SeamCatalog LoadCatalog() =>
        SeamCatalogLoader.Load(Encoding.UTF8.GetBytes(Catalog), "seams.toml");

    private static MemoryPristineSource Pristine() => new(new Dictionary<string, byte[]>
    {
        ["assets/gml/NpcId.gml"] = Encoding.UTF8.GetBytes(NpcEnum),
    });

    // An outgoing archive whose enum file carries generated member lines with
    // the expander's markers, one live and one tombstoned.
    private static string WriteArchive(string enumText)
    {
        var path = Path.Combine(Directory.CreateTempSubdirectory("momi-reseed-test").FullName, "assets.zip");
        using var zip = ZipFile.Open(path, ZipArchiveMode.Create);
        using var writer = new StreamWriter(zip.CreateEntry("assets/gml/NpcId.gml").Open(), new UTF8Encoding(false));
        writer.Write(enumText);
        return path;
    }

    private static ReseedPointReport Point(ReseedCheckResult result) =>
        result.Points.Single(p => p.PointId == "npc_roster");

    [Test]
    public void ShouldReportWhatTheSavesWouldRecover()
    {
        var dir = SaveVaults.WriteSaves(SaveVaults.Pack(("npcs", """{"ari":{},"eiland":{},"modauthor_luna":{}}""")));

        var result = ReseedChecker.Check(dir, Pristine(), catalog: LoadCatalog());

        Assert.That(result.Ok, Is.True, string.Join("; ", result.Notes));
        Assert.That(result.ExitCode, Is.EqualTo(0));
        Assert.That(result.SaveCount, Is.EqualTo(1));
        Assert.That(Point(result).BaseLen, Is.EqualTo(2));
        Assert.That(Point(result).FromSaves, Is.EqualTo(new[] { "modauthor_luna" }));
        Assert.That(Point(result).WouldRecover, Is.EqualTo(new[] { "modauthor_luna" }));
        Assert.That(Point(result).AlreadyInLedger, Is.Empty);
        Assert.That(ReseedChecker.RenderText(result, dir, "pristine", null), Does.Contain("RESULT: OK"));
    }

    [Test]
    public void ShouldSubtractWhatTheLedgerAlreadyHolds()
    {
        var dir = SaveVaults.WriteSaves(SaveVaults.Pack(("npcs", """{"ari":{},"modauthor_luna":{}}""")));
        var ledger = new MemoryExtensionLedger(("npc_roster", new ExtensionAssignment("modauthor_luna", 2, "mod.a")));

        var result = ReseedChecker.Check(dir, Pristine(), ledger: ledger, catalog: LoadCatalog());

        Assert.That(Point(result).WouldRecover, Is.Empty);
        Assert.That(Point(result).AlreadyInLedger, Is.EqualTo(new[] { "modauthor_luna" }));
        Assert.That(result.LedgerChecked, Is.True);
    }

    [Test]
    public void ShouldUnionTheOutgoingArchivesMarkersAndFlagAStaleOne()
    {
        var dir = SaveVaults.WriteSaves();
        var archive = WriteArchive(
            "enum NpcId {\n    Ari,\n    Eiland,\n"
            + "    modauthor_ghost = 2, // mmapi_ext:npc_roster:enum_member:modauthor_ghost:vacant\n"
            + "    eiland = 3, // mmapi_ext:npc_roster:enum_member:eiland\n"
            + "    LEN,\n}\n");

        var result = ReseedChecker.Check(dir, Pristine(), archive, catalog: LoadCatalog());

        Assert.That(Point(result).FromSaves, Is.Empty);
        Assert.That(Point(result).FromArchive, Is.EqualTo(new[] { "eiland", "modauthor_ghost" }));
        Assert.That(Point(result).WouldRecover, Is.EqualTo(new[] { "modauthor_ghost" }),
            "a marker naming a vanilla member is ignored, never re-minted");
        Assert.That(result.Notes, Has.Exactly(1).Contains("defines natively"));
        Assert.That(result.ArchiveChecked, Is.True);
    }

    [Test]
    public void ShouldCarryEveryHarvesterNoteAndFailTheCheckOnOne()
    {
        var dir = SaveVaults.WriteSaves(
            SaveVaults.Pack(("npcs", """{"ari":{},"modauthor_luna":{}}""")),
            Encoding.UTF8.GetBytes("not a vault"));

        var result = ReseedChecker.Check(dir, Pristine(), catalog: LoadCatalog());

        Assert.That(result.Ok, Is.False);
        Assert.That(result.ExitCode, Is.EqualTo(1));
        Assert.That(result.Notes, Has.Exactly(1).Contains("unreadable save skipped"));
        Assert.That(Point(result).WouldRecover, Is.EqualTo(new[] { "modauthor_luna" }),
            "the readable save still contributes");
        var text = ReseedChecker.RenderText(result, dir, "pristine", null);
        Assert.That(text, Does.Contain("RESULT: ATTENTION"));
        Assert.That(text, Does.Contain("unreadable save skipped"));
    }

    [Test]
    public void ShouldFlagADriftedSlotShapeInsteadOfStayingSilent()
    {
        // a status slot that is an object without a type string is the slot
        // shape drifting under the reader, which must surface as a note
        var dir = SaveVaults.WriteSaves(SaveVaults.Pack(("player",
            """{"stats":{"status_effects":[null,{"kind":"modauthor_zeal"}]}}""")));
        var withStatus = Catalog + """

            [[extension]]
            id   = "status_effect"
            file = "gml/StatusEffect.gml"

            [extension.ordinal]
            enum     = "StatusEffectId"
            sentinel = "LEN"

            [[extension.sites]]
            id       = "enum_member"
            kind     = "enum_member"
            template = "{{symbol}} = {{ordinal}},"
            indent   = 4

            [extension.vacancy]
            enum_member = "{{symbol}} = {{ordinal}},"
            """ + "\n";
        var pristine = new MemoryPristineSource(new Dictionary<string, byte[]>
        {
            ["assets/gml/NpcId.gml"] = Encoding.UTF8.GetBytes(NpcEnum),
            ["assets/gml/StatusEffect.gml"] = Encoding.UTF8.GetBytes("enum StatusEffectId {\n    Burn,\n    LEN,\n}\n"),
        });

        var result = ReseedChecker.Check(dir, pristine,
            catalog: SeamCatalogLoader.Load(Encoding.UTF8.GetBytes(withStatus), "seams.toml"));

        Assert.That(result.Ok, Is.False);
        Assert.That(result.Notes, Has.Exactly(1).Contains("unexpected shape"));
        Assert.That(result.Points.Single(p => p.PointId == "status_effect").WouldRecover, Is.Empty);
    }
}
