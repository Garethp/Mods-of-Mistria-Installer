using System.IO.Compression;
using System.Text;
using Garethp.ModsOfMistriaInstallerLib.Operations;
using Garethp.ModsOfMistriaInstallerLib.Seam;
using Newtonsoft.Json.Linq;

namespace ModsOfMistriaInstallerLibTests.Operations;

// Read-only seam verification against a fabricated build zip: a clean build
// passes, a broken anchor, a renamed callee and an absent target file are
// reported, and the text, JSON and exit-code surfaces carry the problems.
// Nothing is written; no install is touched.
[TestFixture]
public class SeamVerifierTest
{
    private const string PristineGame = "function step_begin() {\n}\n";

    private const string PristineOther = "function helper() {\n    return 1;\n}\n\n"
                                         + "function other2() {\n    foo_builtin(1);\n}\n";

    private const string BrokenGame = "function step_begin() {\n    injected();\n}\n";

    private const string CatalogToml = """
        version = 2

        [[hook]]
        name = "game.step_begin"
        kind = "event"
        doc  = "Fires at the top of Game.step_begin()."

        [[hook]]
        name = "test.foo"
        kind = "event"
        doc  = "The call rewrite's hook."

        [[seam]]
        id = "game_step"
        file = "gml/objects/Game.gml"
        anchor = '''
        function step_begin() {
        }'''
        replace = '''
        function step_begin() {
            mmapi_run_installs(); // __momi_test_game_step
        }'''
        marker = "__momi_test_game_step"
        provides = ["game.step_begin"]

        [[engine_fix]]
        name = "other_fix"
        file = "gml/objects/Other.gml"
        anchor = '''
        function helper() {
            return 1;
        }'''
        replace = '''
        function helper() {
            return 1; // __momi_test_other_fix
        }'''
        marker = "__momi_test_other_fix"

        [[call_rewrite]]
        id = "foo_rewrite"
        callee = "foo_builtin"
        to = "mmapi_foo"
        args = 1
        provides = ["test.foo"]
        """;

    private string _root = "";
    private string _zipPath = "";
    private SeamCatalog _catalog = null!;

    [SetUp]
    public void CreateTempDir()
    {
        _root = Path.Combine(Path.GetTempPath(), "momi_verify_" + Path.GetRandomFileName());
        Directory.CreateDirectory(_root);
        _zipPath = Path.Combine(_root, "assets.zip");
        _catalog = SeamCatalogLoader.Load(Encoding.UTF8.GetBytes(CatalogToml), "synthetic");
    }

    [TearDown]
    public void RemoveTempDir()
    {
        Directory.Delete(_root, true);
    }

    // Fabricate the build's assets.zip. `other` null omits Other.gml.
    private void WriteZip(string game, string? other)
    {
        if (File.Exists(_zipPath)) File.Delete(_zipPath);
        using var archive = ZipFile.Open(_zipPath, ZipArchiveMode.Create);
        using (var stream = archive.CreateEntry("assets/gml/objects/Game.gml").Open())
            stream.Write(Encoding.UTF8.GetBytes(game));
        if (other is null) return;
        using var otherStream = archive.CreateEntry("assets/gml/objects/Other.gml").Open();
        otherStream.Write(Encoding.UTF8.GetBytes(other));
    }

    private VerifyResult Verify()
    {
        using var pristine = new ZipPristineSource(_zipPath);
        return SeamVerifier.Verify(pristine, _catalog);
    }

    [Test]
    public void ShouldPassOnACleanBuild()
    {
        WriteZip(PristineGame, PristineOther);

        var result = Verify();

        Assert.That(result.Ok, Is.True);
        Assert.That(result.Problems, Is.Empty);
        Assert.That(result.SeamCount, Is.EqualTo(1));
        Assert.That(result.EngineFixCount, Is.EqualTo(1));
        Assert.That(result.CallRewriteCount, Is.EqualTo(1));
        Assert.That(result.EngineFileCount, Is.EqualTo(2));
    }

    [Test]
    public void ShouldReportABrokenAnchor()
    {
        // a patch rewrote step_begin's body; the anchor no longer matches
        WriteZip(BrokenGame, PristineOther);

        var result = Verify();

        Assert.That(result.Ok, Is.False);
        Assert.That(result.Problems.Select(p => p.Message), Has.Some.Contains("game_step"));
    }

    [Test]
    public void ShouldReportARenamedCallee()
    {
        // anchors still hold, but the engine renamed foo_builtin, so the call
        // rewrite finds zero sites (its own fail-closed branch)
        WriteZip(PristineGame, "function helper() {\n    return 1;\n}\n\n"
                               + "function other2() {\n    renamed_builtin(1);\n}\n");

        var result = Verify();

        Assert.That(result.Ok, Is.False);
        Assert.That(result.Problems.Select(p => p.Message), Has.Some.Contains("foo_rewrite"));
    }

    [Test]
    public void ShouldReportAMissingTargetFile()
    {
        WriteZip(PristineGame, null);

        var result = Verify();

        Assert.That(result.Ok, Is.False);
        Assert.That(result.Problems.Select(p => p.Message), Has.Some.Contains("Other.gml"));
    }

    [Test]
    public void ShouldCarryStructuredProblemRecords()
    {
        WriteZip(BrokenGame, PristineOther);

        var result = Verify();

        Assert.That(result.Problems, Has.Count.EqualTo(1));
        var problem = result.Problems[0];
        Assert.That(problem.Kind, Is.EqualTo(SeamProblemKind.Anchor));
        Assert.That(problem.EntryId, Is.EqualTo("game_step"));
        Assert.That(problem.File, Is.EqualTo("assets/gml/objects/Game.gml"));
        // the anchor's first line survives in the patched build: line 1
        Assert.That(problem.Line, Is.EqualTo(1));
        Assert.That(problem.Context, Does.Contain("function step_begin()"));
        Assert.That(problem.Context, Does.Contain("1  "));
    }

    [Test]
    public void ShouldIncludeTheContextExcerptInTextOutput()
    {
        // no flag needed: the excerpts are the re-anchoring payload
        WriteZip(BrokenGame, PristineOther);

        var text = SeamVerifier.RenderText(Verify(), _zipPath);

        Assert.That(text, Does.Contain("closest match in this build"));
        Assert.That(text, Does.Contain("injected();"));
    }

    [Test]
    public void ShouldCarryProblemRecordsInJson()
    {
        WriteZip(BrokenGame, PristineOther);

        var payload = JObject.Parse(SeamVerifier.ToJson(Verify(), _zipPath));

        Assert.That(payload["ok"]!.Value<bool>(), Is.False);
        Assert.That(payload["source"]!.Value<string>(), Is.EqualTo(_zipPath));
        var records = (JArray)payload["problem_records"]!;
        Assert.That(records, Has.Count.EqualTo(1));
        Assert.That(records[0]!["kind"]!.Value<string>(), Is.EqualTo("anchor"));
        Assert.That(records[0]!["entry_id"]!.Value<string>(), Is.EqualTo("game_step"));
        Assert.That(records[0]!["context"]!.Value<string>(), Is.Not.Empty);
    }

    [Test]
    public void ShouldPinTheExitCodes()
    {
        WriteZip(PristineGame, PristineOther);
        var clean = Verify();
        Assert.That(clean.ExitCode, Is.EqualTo(0));
        Assert.That(SeamVerifier.RenderText(clean, _zipPath), Does.Contain("OK"));

        WriteZip(BrokenGame, PristineOther);
        var broken = Verify();
        Assert.That(broken.ExitCode, Is.EqualTo(1));
        Assert.That(SeamVerifier.RenderText(broken, _zipPath), Does.Contain("FAIL"));
    }

    [Test]
    public void ShouldThrowWhenNoBackupExists()
    {
        Assert.Throws<FileNotFoundException>(() => SeamVerifier.LocateBackup(_root));
    }

    [Test]
    public void ShouldLocateWhicheverBackupNameExists()
    {
        // the seam check never migrates (D14): a not-yet-migrated install's legacy
        // backup name is accepted as it stands, and MOMI's own name wins
        var legacy = Path.Combine(_root, "assets_backup.zip");
        File.WriteAllBytes(legacy, []);
        Assert.That(SeamVerifier.LocateBackup(_root), Is.EqualTo(legacy));

        var backup = Path.Combine(_root, "assets.bak.zip");
        File.WriteAllBytes(backup, []);
        Assert.That(SeamVerifier.LocateBackup(_root), Is.EqualTo(backup));
    }
}
