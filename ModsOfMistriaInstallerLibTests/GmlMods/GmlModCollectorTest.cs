using System.Text;
using Garethp.ModsOfMistriaInstallerLib.GmlMods;
using Garethp.ModsOfMistriaInstallerLib.ModTypes;
using ModsOfMistriaInstallerLibTests.Fixtures;

namespace ModsOfMistriaInstallerLibTests.GmlMods;

[TestFixture]
public class GmlModCollectorTest
{
    private string _root = "";

    [SetUp]
    public void CreateTempDir()
    {
        _root = Path.Combine(Path.GetTempPath(), "momi_collect_" + Path.GetRandomFileName());
        Directory.CreateDirectory(_root);
    }

    [TearDown]
    public void RemoveTempDir()
    {
        Directory.Delete(_root, true);
    }

    private FolderMod ModWithManifest(string manifestJson)
    {
        var dir = Path.Combine(_root, Path.GetRandomFileName());
        Directory.CreateDirectory(Path.Combine(dir, "gml"));
        File.WriteAllText(Path.Combine(dir, "gml", "S.gml"), "// x\n");
        File.WriteAllText(Path.Combine(dir, "manifest.json"), manifestJson);
        return FolderMod.FromManifest(dir);
    }

    [Test]
    public void ShouldCollectTheGmlTreeSortedAndModRelative()
    {
        var mod = new MockMod(new Dictionary<string, string>
        {
            { "gml/core/State.gml", "// state\n" },
            { "gml/core/Alpha.gml", "// alpha\n" },
            { "images/icon.png", "" },
        });

        var code = GmlModCollector.Collect(mod);

        Assert.That(code, Is.Not.Null);
        Assert.That(code!.GmlFiles, Is.EqualTo(new[] { "gml/core/Alpha.gml", "gml/core/State.gml" }));
        Assert.That(code.Read("gml/core/State.gml"), Is.EqualTo(Encoding.UTF8.GetBytes("// state\n")));
    }

    [Test]
    public void ShouldReturnNullWhenTheModShipsNoGml()
    {
        var mod = new MockMod(new Dictionary<string, string> { { "images/icon.png", "" } });

        Assert.That(GmlModCollector.Collect(mod), Is.Null);
    }

    [Test]
    public void ShouldFoldDotsAndDashesIntoTheSymbol()
    {
        var mod = new MockMod(new Dictionary<string, string> { { "gml/Main.gml", "// x\n" } })
        {
            Id = "tester.my-mod",
        };

        var code = GmlModCollector.Collect(mod);

        Assert.That(code!.Symbol, Is.EqualTo("tester_my_mod"));
    }

    [Test]
    public void ShouldCollectFromAFolderMod()
    {
        var mod = ModWithManifest(
            """{"name": "Mod", "version": "1", "author": "Tester", "minInstallerVersion": "0.12"}""");

        var code = GmlModCollector.Collect(mod);

        Assert.That(code, Is.Not.Null);
        Assert.That(code!.Id, Is.EqualTo("tester.mod"));
        Assert.That(code.GmlFiles, Is.EqualTo(new[] { "gml/S.gml" }));
        Assert.That(code.Read("gml/S.gml"), Is.EqualTo(Encoding.UTF8.GetBytes("// x\n")));
    }

    [Test]
    public void ShouldReadRequiredHooksFromTheManifest()
    {
        var mod = ModWithManifest(
            """{"name": "Mod", "version": "1", "author": "t", "minInstallerVersion": "0.12", "requires_hooks": ["game.step_begin"]}""");

        Assert.That(mod.GetRequiredHooks(), Is.EqualTo(new[] { "game.step_begin" }));
        Assert.That(mod.Validate().Errors, Is.Empty);
    }

    [Test]
    public void ShouldRejectAMalformedRequiredHooksField()
    {
        var mod = ModWithManifest(
            """{"name": "Mod", "version": "1", "author": "t", "minInstallerVersion": "0.12", "requires_hooks": [1, 2]}""");

        var validation = mod.Validate();

        Assert.That(validation.Errors.Any(e => e.Message.Contains("requires_hooks")), Is.True);
    }
}
