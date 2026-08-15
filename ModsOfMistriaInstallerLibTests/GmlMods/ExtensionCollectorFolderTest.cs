using System.Text;
using Garethp.ModsOfMistriaInstallerLib.GmlMods;
using Garethp.ModsOfMistriaInstallerLib.ModTypes;
using Garethp.ModsOfMistriaInstallerLib.Seam;

namespace ModsOfMistriaInstallerLibTests.GmlMods;

// The collector against a real FolderMod on disk, not MockMod. MockMod's
// GetAllFiles returns mod-relative keys. FolderMod returns absolute paths and
// Windows separators, and the difference is exactly where a path filter can
// pass its unit test and find nothing in practice.
[TestFixture]
public class ExtensionCollectorFolderTest
{
    private const string Catalog = """
        version = 2

        [[extension]]
        id   = "npc_roster"
        file = "gml/NpcId.gml"

        [extension.ordinal]
        enum     = "NpcId"
        sentinel = "LEN"

        [[extension.fields]]
        name = "object"
        type = "identifier"
        doc  = "The object."

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
    public void CreateMod()
    {
        _root = Path.Combine(Path.GetTempPath(), "momi_extfolder_" + Path.GetRandomFileName());
        Directory.CreateDirectory(Path.Combine(_root, "gml"));
        Directory.CreateDirectory(Path.Combine(_root, "momi", "extensions", "npc_roster"));
        File.WriteAllText(Path.Combine(_root, "gml", "S.gml"), "// x\n");
        File.WriteAllText(Path.Combine(_root, "momi", "extensions", "npc_roster", "echo.toml"),
            "object = \"obj_echo\"\n");
        File.WriteAllText(Path.Combine(_root, "manifest.json"), """
            {
              "name": "ExtFixture",
              "author": "MomiTest",
              "version": "1.0.0",
              "minInstallerVersion": "0.12"
            }
            """);
    }

    [TearDown]
    public void RemoveMod() => Directory.Delete(_root, true);

    private FolderMod Mod() => FolderMod.FromManifest(_root)!;

    [Test]
    public void ShouldSeeRegistrationsThroughAFolderMod()
    {
        Assert.That(ExtensionCollector.HasRegistrations(Mod()), Is.True);
    }

    [Test]
    public void ShouldCollectThroughAFolderMod()
    {
        var catalog = SeamCatalogLoader.Load(Encoding.UTF8.GetBytes(Catalog), "seams.toml");

        var result = ExtensionCollector.Collect(Mod(), catalog);

        Assert.That(result.Problems, Is.Empty);
        Assert.That(result.Registrations.Single().Symbol, Is.EqualTo("momitest_extfixture_echo"));
        Assert.That(result.Registrations.Single().RenderedValues["object"], Is.EqualTo("obj_echo"));
    }

    [Test]
    public void ShouldWarnWhenTheObjectIsNeverCreatedInTheModsGml()
    {
        var catalog = SeamCatalogLoader.Load(Encoding.UTF8.GetBytes(Catalog), "seams.toml");

        var result = ExtensionCollector.Collect(Mod(), catalog);

        var objectFindings = result.Findings.Where(f => f.Message.Contains("names object")).ToList();
        Assert.That(objectFindings, Has.Count.EqualTo(1));
        Assert.That(objectFindings[0].Message, Does.Contain("'obj_echo'"));
        Assert.That(objectFindings[0].Message, Does.Contain("fine when creation is indirect"));
    }

    [Test]
    public void ShouldStaySilentWhenTheModCreatesItsObject()
    {
        File.WriteAllText(Path.Combine(_root, "gml", "Echo.gml"),
            "object_create(\"obj_echo\", object_reserve(\"par_NPC\"), {});\n");
        var catalog = SeamCatalogLoader.Load(Encoding.UTF8.GetBytes(Catalog), "seams.toml");

        var result = ExtensionCollector.Collect(Mod(), catalog);

        Assert.That(result.Findings.Where(f => f.Message.Contains("names object")), Is.Empty);
    }

    [Test]
    public void ShouldWarnWhenTheFiddleCompanionOmitsTheSpringOutfit()
    {
        Directory.CreateDirectory(Path.Combine(_root, "fiddle", "npcs"));
        File.WriteAllText(Path.Combine(_root, "fiddle", "npcs", "momitest_extfixture_echo.toml"),
            "name = \"Echo\"\noutfits = [\"summer\", \"winter\"]\n");
        var catalog = SeamCatalogLoader.Load(Encoding.UTF8.GetBytes(Catalog), "seams.toml");

        var result = ExtensionCollector.Collect(Mod(), catalog);

        var outfitFindings = result.Findings.Where(f => f.Message.Contains("spring")).ToList();
        Assert.That(outfitFindings, Has.Count.EqualTo(1));
        Assert.That(outfitFindings[0].Message, Does.Contain("wardrobe key that does not exist"));
    }

    [Test]
    public void ShouldStaySilentWhenTheFiddleDeclaresASpringOutfit()
    {
        Directory.CreateDirectory(Path.Combine(_root, "fiddle", "npcs"));
        File.WriteAllText(Path.Combine(_root, "fiddle", "npcs", "momitest_extfixture_echo.toml"),
            "name = \"Echo\"\noutfits = [\"spring\", \"summer\"]\n");
        var catalog = SeamCatalogLoader.Load(Encoding.UTF8.GetBytes(Catalog), "seams.toml");

        var result = ExtensionCollector.Collect(Mod(), catalog);

        Assert.That(result.Findings.Where(f => f.Message.Contains("spring")), Is.Empty);
    }
}
