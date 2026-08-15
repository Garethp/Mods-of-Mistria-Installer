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

    [Test]
    public void ShouldRenderLedgerVacanciesEvenWithZeroMods()
    {
        // The tombstone contract is load-bearing precisely when every mod is
        // gone, because a save's name references (date photos) resolve only
        // while the enum member exists.
        var catalogPath = Path.Combine(_fom, "catalog.toml");
        File.WriteAllText(catalogPath, SyntheticLayer.CatalogToml + "\n" + """

            [[extension]]
            id   = "roster"
            file = "gml/objects/Other.gml"

            [extension.ordinal]
            enum     = "Thing"
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

            [[extension.vacancy_files]]
            path    = "data/{{symbol}}.toml"
            content = "vacant = true\n"
            """ + "\n");

        // pristine carries the ordinal enum
        var livePath = Path.Combine(_fom, "assets.zip");
        File.Delete(livePath);
        using (var archive = ZipFile.Open(livePath, ZipArchiveMode.Create))
        {
            WriteEntry(archive, "assets/gml/objects/Game.gml", SyntheticLayer.PristineGame);
            WriteEntry(archive, "assets/gml/objects/Other.gml",
                "enum Thing {\n    Alpha,\n    LEN\n}\n\n" + SyntheticLayer.PristineOther);
            WriteEntry(archive, "assets/fiddle/locations.toml", "");
        }

        // a tombstone with no mod behind it
        File.WriteAllText(Path.Combine(_fom, ExtensionLedgerStore.FileName),
            """{"version":1,"points":{"roster":{"assigned":[{"symbol":"gone_luna","ordinal":1,"mod":"gone.mod"}]}}}""");

        var result = new ModInstaller(_fom, "").InstallMods([], (_, _) => { },
            gateMode: CompileGateMode.Off);

        Assert.That(result.Summary(), Is.EqualTo("0 mod(s) installed"));
        using var live = ZipFile.OpenRead(new AssetsStore(_fom).LivePath);
        Assert.That(ReadEntry(live, "assets/gml/objects/Other.gml"),
            Does.Contain("gone_luna = 1, // mmapi_ext:roster:enum_member:gone_luna:vacant"));
        Assert.That(live.GetEntry("assets/data/gone_luna.toml"), Is.Not.Null,
            "the vacancy's data stub must be emitted");
        Assert.That(ReadEntry(live, ExtensionRegistryRenderer.RegistryRel),
            Does.Contain("\"gone_luna\", 1,"));
    }

    [Test]
    public void ShouldRebaseAutomaticallyWhenTheBaseEnumGrew()
    {
        // A game update that grows the base enum into assigned ordinals
        // reassigns them at the next
        // install, logged, instead of failing until a manual CLI run. The
        // reassignment is save-invisible because saves reference names.
        var catalogPath = Path.Combine(_fom, "catalog.toml");
        File.WriteAllText(catalogPath, SyntheticLayer.CatalogToml + "\n" + """

            [[extension]]
            id   = "roster"
            file = "gml/objects/Other.gml"

            [extension.ordinal]
            enum     = "Thing"
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

            [[extension.vacancy_files]]
            path    = "data/{{symbol}}.toml"
            content = "vacant = true\n"
            """ + "\n");

        // The base enum has grown a member since the ledger assigned
        // ordinal 1, so Alpha and Beta now occupy 0 and 1.
        var livePath = Path.Combine(_fom, "assets.zip");
        File.Delete(livePath);
        using (var archive = ZipFile.Open(livePath, ZipArchiveMode.Create))
        {
            WriteEntry(archive, "assets/gml/objects/Game.gml", SyntheticLayer.PristineGame);
            WriteEntry(archive, "assets/gml/objects/Other.gml",
                "enum Thing {\n    Alpha,\n    Beta,\n    LEN\n}\n\n" + SyntheticLayer.PristineOther);
            WriteEntry(archive, "assets/fiddle/locations.toml", "");
        }

        File.WriteAllText(Path.Combine(_fom, ExtensionLedgerStore.FileName),
            """{"version":1,"points":{"roster":{"assigned":[{"symbol":"gone_luna","ordinal":1,"mod":"gone.mod"}]}}}""");

        var result = new ModInstaller(_fom, "").InstallMods([], (_, _) => { },
            gateMode: CompileGateMode.Off);

        Assert.That(result.Summary(), Is.EqualTo("0 mod(s) installed"));
        using var live = ZipFile.OpenRead(new AssetsStore(_fom).LivePath);
        Assert.That(ReadEntry(live, "assets/gml/objects/Other.gml"),
            Does.Contain("gone_luna = 2, // mmapi_ext:roster:enum_member:gone_luna:vacant"),
            "the tombstone must render above the grown base enum");

        var saved = JObject.Parse(File.ReadAllText(Path.Combine(_fom, ExtensionLedgerStore.FileName)));
        var assignment = saved["points"]!["roster"]!["assigned"]![0]!;
        Assert.That((int)assignment["ordinal"]!, Is.EqualTo(2), "the rebased ordinal must persist");
        Assert.That((string)assignment["mod"]!, Is.EqualTo("gone.mod"), "attribution must survive the rebase");
    }

    // The synthetic extension catalog with the point named npc_roster, so the
    // save-harvest rule table recognises it end to end.
    private const string NpcRosterCatalogTail = """

        [[extension]]
        id   = "npc_roster"
        file = "gml/objects/Other.gml"

        [extension.ordinal]
        enum     = "Thing"
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

        [[extension.vacancy_files]]
        path    = "data/{{symbol}}.toml"
        content = "vacant = true\n"
        """;

    private void WriteExtensionFixture(string ledgerJson, string pointId = "roster")
    {
        var catalogPath = Path.Combine(_fom, "catalog.toml");
        var tail = pointId == "roster"
            ? NpcRosterCatalogTail.Replace("id   = \"npc_roster\"", "id   = \"roster\"")
            : NpcRosterCatalogTail;
        File.WriteAllText(catalogPath, SyntheticLayer.CatalogToml + "\n" + tail + "\n");

        var livePath = Path.Combine(_fom, "assets.zip");
        File.Delete(livePath);
        using (var archive = ZipFile.Open(livePath, ZipArchiveMode.Create))
        {
            WriteEntry(archive, "assets/gml/objects/Game.gml", SyntheticLayer.PristineGame);
            WriteEntry(archive, "assets/gml/objects/Other.gml",
                "enum Thing {\n    Alpha,\n    LEN\n}\n\n" + SyntheticLayer.PristineOther);
            WriteEntry(archive, "assets/fiddle/locations.toml", "");
        }

        if (ledgerJson.Length > 0)
            File.WriteAllText(Path.Combine(_fom, ExtensionLedgerStore.FileName), ledgerJson);
    }

    [Test]
    public void ShouldRefuseToCommitVanillaWhenTheLedgerCannotStage()
    {
        // The vanilla-commit guard. A ledger fault that fails staging must stop the
        // install, never commit a vanilla archive that strips the tombstones
        // and tolerance seams while stamped saves still name them. The
        // duplicate symbol here is a fault the automatic rebase cannot repair.
        WriteExtensionFixture(
            """{"version":1,"points":{"roster":{"assigned":[{"symbol":"gone_luna","ordinal":1,"mod":"gone.mod"},{"symbol":"gone_luna","ordinal":2,"mod":"gone.mod"}]}}}""");
        var ledgerBefore = File.ReadAllText(Path.Combine(_fom, ExtensionLedgerStore.FileName));

        var exception = Assert.Throws<InvalidOperationException>(() =>
            new ModInstaller(_fom, "").InstallMods([], (_, _) => { }, gateMode: CompileGateMode.Off));

        Assert.That(exception!.Message, Does.Contain("ledger holds assignments"));
        Assert.That(File.ReadAllText(Path.Combine(_fom, ExtensionLedgerStore.FileName)),
            Is.EqualTo(ledgerBefore), "a refused install must not rewrite the ledger");
        using var live = ZipFile.OpenRead(new AssetsStore(_fom).LivePath);
        Assert.That(live.GetEntry("manifest.toml"), Is.Null,
            "the archive must not have been rebuilt");
    }

    [Test]
    public void ShouldReseedALostLedgerFromSavesEndToEnd()
    {
        // The full reseed union through the installer. A save names a symbol
        // the (absent) ledger does not, the install recovers it as a vacancy
        // attributed "recovered", and a second install is a byte-level no-op.
        WriteExtensionFixture("", pointId: "npc_roster");
        var savesDir = Path.Combine(_fom, "saves");
        Directory.CreateDirectory(savesDir);
        File.WriteAllBytes(Path.Combine(savesDir, "game-1-0.sav"),
            PackSave(("npcs", """{"alpha":{},"gone_luna":{}}""")));

        new ModInstaller(_fom, "", savesDir).InstallMods([], (_, _) => { },
            gateMode: CompileGateMode.Off);

        var ledgerPath = Path.Combine(_fom, ExtensionLedgerStore.FileName);
        var saved = JObject.Parse(File.ReadAllText(ledgerPath));
        var assignment = saved["points"]!["npc_roster"]!["assigned"]!.Single();
        Assert.That((string)assignment["symbol"]!, Is.EqualTo("gone_luna"));
        Assert.That((int)assignment["ordinal"]!, Is.EqualTo(1));
        Assert.That((string)assignment["mod"]!, Is.EqualTo("recovered"));
        using (var live = ZipFile.OpenRead(new AssetsStore(_fom).LivePath))
        {
            Assert.That(ReadEntry(live, "assets/gml/objects/Other.gml"),
                Does.Contain("gone_luna = 1, // mmapi_ext:npc_roster:enum_member:gone_luna:vacant"));
        }

        var ledgerAfterFirst = File.ReadAllText(ledgerPath);
        new ModInstaller(_fom, "", savesDir).InstallMods([], (_, _) => { },
            gateMode: CompileGateMode.Off);
        Assert.That(File.ReadAllText(ledgerPath), Is.EqualTo(ledgerAfterFirst),
            "the union is idempotent, so the second install must not rewrite the ledger");
    }

    [Test]
    public void ShouldUnionArchiveMarkersAndRejectOnesTheGameNowDefines()
    {
        // The archive half of the union, end to end. A marker in the outgoing
        // (marked) archive recovers, and a marker naming a pristine member is
        // rejected instead of re-minted. The backup carries the pristine enum
        // and the live archive is marked, so EnsureBackup leaves it alone.
        WriteExtensionFixture("", pointId: "npc_roster");
        var livePath = Path.Combine(_fom, "assets.zip");
        File.Copy(livePath, Path.Combine(_fom, "assets.bak.zip"));
        File.Delete(livePath);
        using (var archive = ZipFile.Open(livePath, ZipArchiveMode.Create))
        {
            WriteEntry(archive, "manifest.toml", "");
            WriteEntry(archive, "assets/gml/objects/Game.gml", SyntheticLayer.PristineGame);
            WriteEntry(archive, "assets/gml/objects/Other.gml",
                "enum Thing {\n    Alpha,\n"
                + "    gone_rex = 1, // mmapi_ext:npc_roster:enum_member:gone_rex:vacant\n"
                + "    alpha = 2, // mmapi_ext:npc_roster:enum_member:alpha\n"
                + "    LEN\n}\n\n" + SyntheticLayer.PristineOther);
            WriteEntry(archive, "assets/fiddle/locations.toml", "");
        }

        var savesDir = Path.Combine(_fom, "saves");
        Directory.CreateDirectory(savesDir);

        new ModInstaller(_fom, "", savesDir).InstallMods([], (_, _) => { },
            gateMode: CompileGateMode.Off);

        var saved = JObject.Parse(File.ReadAllText(Path.Combine(_fom, ExtensionLedgerStore.FileName)));
        var assignment = saved["points"]!["npc_roster"]!["assigned"]!.Single();
        Assert.That((string)assignment["symbol"]!, Is.EqualTo("gone_rex"),
            "the marker symbol recovers, and 'alpha' must not because the game defines it natively");
        Assert.That((string)assignment["mod"]!, Is.EqualTo("recovered"));
    }

    [Test]
    public void ShouldReattributeARecoveredSymbolWhenItsModReturns()
    {
        // A reseed minted the tombstone as "recovered". The mod coming back
        // adopts its ordinal and takes its attribution back.
        WriteExtensionFixture(
            """{"version":1,"points":{"npc_roster":{"assigned":[{"symbol":"tester_mymod_luna","ordinal":1,"mod":"recovered"}]}}}""",
            pointId: "npc_roster");
        var mod = new MockMod(new Dictionary<string, object>
        {
            ["momi/extensions/npc_roster/luna.toml"] = "object = \"tester_mymod_luna_obj\"\n",
        })
        {
            Id = "tester.mymod", Name = "mymod", Author = "tester",
            DirName = "mymod", Version = "0.0.1",
        };

        var result = new ModInstaller(_fom, "").InstallMods([mod], (_, _) => { },
            gateMode: CompileGateMode.Off);

        Assert.That(result.Skipped, Is.Empty);
        var saved = JObject.Parse(File.ReadAllText(Path.Combine(_fom, ExtensionLedgerStore.FileName)));
        var assignment = saved["points"]!["npc_roster"]!["assigned"]!.Single();
        Assert.That((int)assignment["ordinal"]!, Is.EqualTo(1), "adoption keeps the ordinal");
        Assert.That((string)assignment["mod"]!, Is.EqualTo("tester.mymod"),
            "the returning mod reclaims attribution from the recovered placeholder");
    }

    // The engine's save container in miniature, one zlib stream over a u64le
    // record count, then per record u64le name length, name, u64le body
    // length, body. Mirrors the harvester tests' writer.
    private static byte[] PackSave(params (string Name, string Body)[] records)
    {
        using var plain = new MemoryStream();
        void U64(ulong value)
        {
            Span<byte> buffer = stackalloc byte[8];
            System.Buffers.Binary.BinaryPrimitives.WriteUInt64LittleEndian(buffer, value);
            plain.Write(buffer);
        }

        U64((ulong)records.Length);
        foreach (var (name, body) in records)
        {
            var nameBytes = Encoding.UTF8.GetBytes(name);
            U64((ulong)nameBytes.Length);
            plain.Write(nameBytes);
            var bodyBytes = Encoding.UTF8.GetBytes(body);
            U64((ulong)bodyBytes.Length);
            plain.Write(bodyBytes);
        }

        using var packed = new MemoryStream();
        using (var deflate = new System.IO.Compression.ZLibStream(packed,
                   System.IO.Compression.CompressionLevel.Fastest, true))
            deflate.Write(plain.ToArray());
        return packed.ToArray();
    }

    private static MockMod GmlMod(string id, List<string>? requiresHooks = null) =>
        new(new Dictionary<string, object> { { "gml/core/State.gml", "// state\n" } })
        {
            Id = id,
            Name = id,
            Author = "tester",
            DirName = id,
            Version = "0.0.1",
            RequiredHooks = requiresHooks ?? [],
        };

    private static MockMod ContentMod(string id) =>
        new(new Dictionary<string, object>()) { Id = id, Name = id, Author = "tester" };

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
