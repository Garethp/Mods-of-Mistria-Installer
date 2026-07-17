using System.IO.Compression;
using System.Text;
using Garethp.ModsOfMistriaInstallerLib;
using Garethp.ModsOfMistriaInstallerLib.GmlMods;
using Garethp.ModsOfMistriaInstallerLib.ModTypes;
using Garethp.ModsOfMistriaInstallerLib.Seam;
using Garethp.ModsOfMistriaInstallerLib.Store;
using Garethp.ModsOfMistriaInstallerLib.Tools;
using ModsOfMistriaInstallerLibTests.Fixtures;
using ModsOfMistriaInstallerLibTests.TestUtils;
using Newtonsoft.Json.Linq;

namespace ModsOfMistriaInstallerLibTests;

// ModInstaller's wiring over a synthetic store: the layer stages before the
// rebuild, an excluded mod is skipped whole with its content, and the game
// manifest is written after the commit. The layer's own semantics live in
// GmlLayerTest. These cases pin the joins.
[TestFixture]
public class ModInstallerTest
{
    private string _fom = "";
    private string _configDir = "";

    [SetUp]
    public void CreateSyntheticInstall()
    {
        _fom = Path.Combine(Path.GetTempPath(), "momi_install_" + Path.GetRandomFileName());
        _configDir = Path.Combine(_fom, "config");
        Directory.CreateDirectory(_fom);
        Directory.CreateDirectory(_configDir);

        WriteLiveArchive(SyntheticLayer.PristineGame);

        var catalogPath = Path.Combine(_fom, "catalog.toml");
        File.WriteAllText(catalogPath, SyntheticLayer.CatalogToml);
        Environment.SetEnvironmentVariable("MOMI_SEAM_CATALOG", catalogPath);
        Environment.SetEnvironmentVariable("MOMI_GAME_CONFIG_DIR", _configDir);
    }

    [TearDown]
    public void RemoveSyntheticInstall()
    {
        Environment.SetEnvironmentVariable("MOMI_SEAM_CATALOG", null);
        Environment.SetEnvironmentVariable("MOMI_GAME_CONFIG_DIR", null);
        Directory.Delete(_fom, true);
    }

    [Test]
    public void ShouldInstallTheLayerAndTheManifests()
    {
        var gmlMod = GmlMod("testmod");
        var contentMod = ContentMod("contentmod");

        var result = new ModInstaller(_fom, "").InstallMods([gmlMod, contentMod], (_, _) => { },
            gateMode: CompileGateMode.Off);

        Assert.That(result.Installed, Is.EqualTo(new IMod[] { gmlMod, contentMod }));
        Assert.That(result.Skipped, Is.Empty);
        Assert.That(result.Summary(), Is.EqualTo("2 mod(s) installed"));

        using (var live = ZipFile.OpenRead(new AssetsStore(_fom).LivePath))
        {
            // The marker appears only in seamed text
            Assert.That(ReadEntry(live, "assets/gml/objects/Game.gml"), Does.Contain("__momi_test_game_step"));
            Assert.That(live.GetEntry("assets/gml/scripts/mmapi/mmapi.gml"), Is.Not.Null);
            Assert.That(live.GetEntry(SeamStager.HookCatalogRel), Is.Not.Null);
            Assert.That(live.GetEntry("assets/gml/scripts/testmod/core/State.gml"), Is.Not.Null);
            Assert.That(live.GetEntry("manifest.toml"), Is.Not.Null);
        }

        Assert.That(GameManifestIds(), Is.EqualTo(new[] { "testmod", "contentmod" }));
    }

    [Test]
    public void ShouldSkipAnExcludedModWholeAndReportIt()
    {
        var good = GmlMod("testmod");
        var bad = GmlMod("othermod", requiresHooks: ["absent.hook"]);

        var result = new ModInstaller(_fom, "").InstallMods([good, bad], (_, _) => { },
            gateMode: CompileGateMode.Off);

        Assert.That(result.Installed, Is.EqualTo(new IMod[] { good }));
        Assert.That(result.Skipped, Has.Count.EqualTo(1));
        Assert.That(result.Skipped[0].Id, Is.EqualTo("othermod"));
        Assert.That(result.Skipped[0].Reasons, Has.Some.Contains("absent.hook"));
        Assert.That(result.Summary(), Is.EqualTo("1 mod(s) installed, 1 skipped"));

        // The reasons also land as Validation errors on the mod itself
        Assert.That(bad.GetValidation().Errors.Any(e => e.Message.Contains("absent.hook")), Is.True);

        using (var live = ZipFile.OpenRead(new AssetsStore(_fom).LivePath))
        {
            Assert.That(live.GetEntry("assets/gml/scripts/testmod/core/State.gml"), Is.Not.Null);
            Assert.That(live.Entries.Any(e => e.FullName.StartsWith("assets/gml/scripts/othermod/")), Is.False,
                "an excluded mod's gml must not land");
        }

        Assert.That(GameManifestIds(), Is.EqualTo(new[] { "testmod" }));
    }

    [Test]
    public void ShouldNotStageTheLayerWithoutGmlMods()
    {
        // A missing catalog override fails loudly on resolve: a passing
        // install proves the layer never staged
        Environment.SetEnvironmentVariable("MOMI_SEAM_CATALOG", Path.Combine(_fom, "missing-catalog.toml"));

        var contentMod = ContentMod("contentmod");
        var result = new ModInstaller(_fom, "").InstallMods([contentMod], (_, _) => { },
            gateMode: CompileGateMode.Off);

        Assert.That(result.Installed, Is.EqualTo(new IMod[] { contentMod }));
        Assert.That(result.Summary(), Is.EqualTo("1 mod(s) installed"));

        using var live = ZipFile.OpenRead(new AssetsStore(_fom).LivePath);
        Assert.That(live.GetEntry("assets/gml/scripts/mmapi/mmapi.gml"), Is.Null);
        Assert.That(live.GetEntry("manifest.toml"), Is.Not.Null);
    }

    [Test]
    public void ShouldSkipEveryGmlModWhenTheGameGmlChanged()
    {
        // The engine updated: pristine Game.gml no longer matches the anchor.
        // The GML mods are skipped whole and the content-only install proceeds.
        WriteLiveArchive("function step_begin() {\n    NEW_ENGINE_LINE();\n}\n");
        var gmlMod = GmlMod("testmod");
        var contentMod = ContentMod("contentmod");

        var result = new ModInstaller(_fom, "").InstallMods([gmlMod, contentMod], (_, _) => { },
            gateMode: CompileGateMode.Off);

        Assert.That(result.Installed, Is.EqualTo(new IMod[] { contentMod }));
        Assert.That(result.Skipped, Has.Count.EqualTo(1));
        Assert.That(result.Skipped[0].Id, Is.EqualTo("testmod"));
        Assert.That(result.Skipped[0].Reasons, Has.Some.Contains("Game GML changed"));
        Assert.That(gmlMod.GetValidation().Errors.Any(e => e.Message.Contains("Game GML changed")), Is.True);

        using (var live = ZipFile.OpenRead(new AssetsStore(_fom).LivePath))
        {
            Assert.That(live.GetEntry("assets/gml/scripts/mmapi/mmapi.gml"), Is.Null,
                "the layer must not stage against a moved game build");
            Assert.That(live.Entries.Any(e => e.FullName.StartsWith("assets/gml/scripts/testmod/")), Is.False);
            Assert.That(live.GetEntry("manifest.toml"), Is.Not.Null);
        }

        Assert.That(GameManifestIds(), Is.EqualTo(new[] { "contentmod" }));
    }

    [Test]
    public void ShouldLeaveTheLiveArchiveUntouchedWhenFailOnSkipIsSet()
    {
        // fail-on-skip keeps the hard stop: the stage aborts before the
        // rebuild, so a failed stage costs no copy
        WriteLiveArchive("function step_begin() {\n    NEW_ENGINE_LINE();\n}\n");
        var livePath = new AssetsStore(_fom).LivePath;
        var before = File.ReadAllBytes(livePath);

        Assert.Throws<SeamStagingException>(() =>
            new ModInstaller(_fom, "").InstallMods([GmlMod("testmod")], (_, _) => { },
                new GmlLayerOptions { FailOnSkip = true }, CompileGateMode.Off));

        Assert.That(File.ReadAllBytes(livePath), Is.EqualTo(before));
    }

    [Test]
    public void ShouldReportTheModAndPhaseOnTheCoarseChannel()
    {
        var phases = new List<(string Mod, string Phase)>();

        new ModInstaller(_fom, "").InstallMods([GmlMod("testmod")], (_, _) => { },
            gateMode: CompileGateMode.Off,
            reportPhase: (mod, phaseText) => phases.Add((mod, phaseText)));

        // Whole-install steps carry no mod name; per-mod steps carry the mod's
        Assert.That(phases, Has.Some.EqualTo(("", "Preparing GML layer")));
        Assert.That(phases, Has.Some.EqualTo(("testmod", "Installing Images")));
        Assert.That(phases, Has.Some.EqualTo(("", "Writing game archive")));
    }

    [Test]
    public void ShouldResetTheGameManifestOnUninstall()
    {
        var installer = new ModInstaller(_fom, "");
        installer.InstallMods([GmlMod("testmod")], (_, _) => { }, gateMode: CompileGateMode.Off);

        installer.Uninstall();

        var store = new AssetsStore(_fom);
        Assert.That(File.ReadAllBytes(store.LivePath), Is.EqualTo(File.ReadAllBytes(store.BackupPath)));
        Assert.That(GameManifestIds(), Is.Empty);
    }

    [Test]
    public void ShouldRefuseAMissingInstallDirectory()
    {
        Assert.Throws<DirectoryNotFoundException>(() =>
            new ModInstaller(Path.Combine(_fom, "not-there"), "").InstallMods([], (_, _) => { }));
    }

    // ── Fixtures and helpers ───────────────────────────────────────────────────

    private static MockMod GmlMod(string id, List<string>? requiresHooks = null) =>
        new(new Dictionary<string, string> { { "gml/core/State.gml", "// state\n" } })
        {
            Id = id,
            Name = id,
            Author = "tester",
            DirName = id,
            Version = "0.0.1",
            RequiredHooks = requiresHooks ?? [],
        };

    private static MockMod ContentMod(string id) =>
        new(new Dictionary<string, string>()) { Id = id, Name = id, Author = "tester" };

    // The synthetic pristine engine, plus the empty vanilla locations table
    // the location pre-pass reads unconditionally
    private void WriteLiveArchive(string game)
    {
        var livePath = Path.Combine(_fom, "assets.zip");
        File.Delete(livePath);
        using var archive = ZipFile.Open(livePath, ZipArchiveMode.Create);
        WriteEntry(archive, "assets/gml/objects/Game.gml", game);
        WriteEntry(archive, "assets/gml/objects/Other.gml", SyntheticLayer.PristineOther);
        WriteEntry(archive, "assets/fiddle/locations.toml", "");
    }

    private string[] GameManifestIds()
    {
        var manifest = JObject.Parse(File.ReadAllText(Path.Combine(_configDir, "mods", "manifest.json")));
        return manifest["mods"]!.Select(m => m["id"]!.ToString()).ToArray();
    }

    private static void WriteEntry(ZipArchive archive, string name, string content)
    {
        using var stream = archive.CreateEntry(name).Open();
        stream.Write(Encoding.UTF8.GetBytes(content));
    }

    private static string ReadEntry(ZipArchive archive, string name)
    {
        using var stream = archive.GetEntry(name)!.Open();
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}
