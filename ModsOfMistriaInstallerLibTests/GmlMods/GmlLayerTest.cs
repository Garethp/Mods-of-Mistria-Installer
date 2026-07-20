using System.IO.Compression;
using System.Text;
using Garethp.ModsOfMistriaInstallerLib.GmlMods;
using Garethp.ModsOfMistriaInstallerLib.Seam;
using Garethp.ModsOfMistriaInstallerLib.Store;
using ModsOfMistriaInstallerLibTests.TestUtils;

namespace ModsOfMistriaInstallerLibTests.GmlMods;

[TestFixture]
public class GmlLayerTest
{
    private static GmlLayerPlan Stage(GmlLayerOptions? options = null, ScriptedGate? gate = null,
        params GmlModCode[] mods) =>
        GmlLayer.Stage(SyntheticLayer.Catalog(), SyntheticLayer.Pristine(),
            mods.Length > 0 ? mods : [SyntheticLayer.Mod("testmod")], gate, options);

    [Test]
    public void ShouldStageEverything()
    {
        var plan = Stage();

        var game = plan.Seamed["assets/gml/objects/Game.gml"].Text;
        Assert.That(game, Does.Contain("__momi_test_game_step"));
        Assert.That(game, Does.Contain("mmapi_run_installs();"));
        Assert.That(plan.Seamed["assets/gml/objects/Other.gml"].Text, Does.Contain("__momi_test_other_fix"));

        Assert.That(plan.Added, Contains.Key("assets/gml/scripts/mmapi/mmapi.gml"));
        Assert.That(plan.Added, Contains.Key("assets/gml/scripts/mmapi/mmapi_debug.gml"));
        Assert.That(plan.Added, Contains.Key("assets/gml/scripts/testmod/core/State.gml"));

        var hookCatalog = Encoding.UTF8.GetString(plan.Added[SeamStager.HookCatalogRel]);
        Assert.That(hookCatalog, Does.Contain("\"game.step_begin\", \"event\","));

        Assert.That(plan.Survivors.Select(m => m.Id), Is.EqualTo(new[] { "testmod" }));
    }

    [Test]
    public void ShouldFailStagingOnAStaleAnchor()
    {
        // The engine updated: pristine Game.gml no longer matches the anchor
        var pristine = SyntheticLayer.Pristine(game: "function step_begin() {\n    NEW_ENGINE_LINE();\n}\n");

        var exception = Assert.Throws<SeamStagingException>(() =>
            GmlLayer.Stage(SyntheticLayer.Catalog(), pristine, [SyntheticLayer.Mod("testmod")], null));

        Assert.That(exception!.Message, Does.Contain("game_step"));
    }

    [Test]
    public void ShouldReportEveryStagingProblemAtOnce()
    {
        var pristine = SyntheticLayer.Pristine(
            game: "function step_begin_v2() {\n}\n",
            other: "function helper_v2() {\n}\n");

        var exception = Assert.Throws<SeamStagingException>(() =>
            GmlLayer.Stage(SyntheticLayer.Catalog(), pristine, [SyntheticLayer.Mod("testmod")], null));

        Assert.That(exception!.Message, Does.Contain("game_step"));
        Assert.That(exception.Message, Does.Contain("other_fix"));
    }

    [Test]
    public void ShouldKeepThePristineLineEndings()
    {
        var pristine = SyntheticLayer.Pristine(game: SyntheticLayer.PristineGame.Replace("\n", "\r\n"));

        var plan = GmlLayer.Stage(SyntheticLayer.Catalog(), pristine, [SyntheticLayer.Mod("testmod")], null);

        var game = plan.Seamed["assets/gml/objects/Game.gml"].Encode();
        Assert.That(Encoding.UTF8.GetString(game), Does.Contain("__momi_test_game_step"));
        Assert.That(Encoding.UTF8.GetString(game), Does.Contain("\r\n"));
        Assert.That(Encoding.UTF8.GetString(game).Replace("\r\n", ""), Does.Not.Contain("\n"));

        var other = plan.Seamed["assets/gml/objects/Other.gml"].Encode();
        Assert.That(Encoding.UTF8.GetString(other), Does.Not.Contain("\r\n"));
    }

    [Test]
    public void ShouldExcludeAModRequiringAMissingHook()
    {
        var needy = SyntheticLayer.Mod("needy", requiresHooks: ["game.step_begin", "game.ghost"]);

        var plan = Stage(null, null, SyntheticLayer.Mod("testmod"), needy);

        Assert.That(plan.Excluded.Select(e => e.Mod.Id), Is.EqualTo(new[] { "needy" }));
        Assert.That(plan.Excluded[0].Reasons[0], Does.Contain("game.ghost"));
        Assert.That(plan.Added.Keys, Has.None.StartWith("assets/gml/scripts/needy/"));
        Assert.That(plan.Added, Contains.Key("assets/gml/scripts/testmod/core/State.gml"));
    }

    [Test]
    public void ShouldSatisfyRequiredHooksThroughAliases()
    {
        var direct = SyntheticLayer.Mod("direct", requiresHooks: ["game.step_begin"]);
        var aliased = SyntheticLayer.Mod("aliased", requiresHooks: ["game.old_name"]);

        var plan = Stage(null, null, direct, aliased);

        Assert.That(plan.Excluded, Is.Empty);
        Assert.That(plan.Survivors.Select(m => m.Id), Is.EquivalentTo(new[] { "direct", "aliased" }));
    }

    [Test]
    public void ShouldWarnOnAnUnknownHookAndStillApply()
    {
        var mod = SyntheticLayer.Mod("testmod",
            "mmapi_on(\"game.step_begn\", testmod_handle_step);\n");

        var plan = Stage(null, null, mod);

        var warnings = plan.Findings.Where(f => f.Message.Contains("unknown hook")).ToList();
        Assert.That(warnings, Has.Count.EqualTo(1));
        Assert.That(warnings[0].Message, Does.Contain("game.step_begn"));
        Assert.That((warnings[0].File, warnings[0].Line), Is.EqualTo(("gml/core/State.gml", 1)));
        Assert.That(plan.Survivors, Has.Count.EqualTo(1));
    }

    [Test]
    public void ShouldReportAKindMismatch()
    {
        var mod = SyntheticLayer.Mod("testmod",
            "mmapi_filter(\"game.step_begin\", testmod_handle_step);\n");

        var plan = Stage(null, null, mod);

        Assert.That(plan.Findings.Count(f => f.Message.Contains("use mmapi_on")), Is.EqualTo(1));
        Assert.That(plan.Survivors, Has.Count.EqualTo(1));
    }

    [Test]
    public void ShouldExcludeOnAnUnknownHookUnderStrictLints()
    {
        var mod = SyntheticLayer.Mod("testmod",
            "mmapi_on(\"game.step_begn\", testmod_handle_step);\n");

        var plan = Stage(new GmlLayerOptions { StrictLints = true }, null, mod);

        Assert.That(plan.Excluded.Select(e => e.Mod.Id), Is.EqualTo(new[] { "testmod" }));
        Assert.That(plan.Excluded[0].Reasons[0], Does.Contain("strict-lints"));
        Assert.That(plan.Added.Keys, Has.None.StartWith("assets/gml/scripts/testmod/"));
    }

    [Test]
    public void ShouldWarnOnSymbolFindingsAndStillApply()
    {
        var mod = SyntheticLayer.Mod("testmod",
            "function clamp01(x) {\n}\n"
            + "function testmod_boot() {\n    global.__mmapi_hax = 1;\n}\n");

        var plan = Stage(null, null, mod);

        Assert.That(plan.Findings.Any(f => f.Message.Contains("not namespaced") && f.Message.Contains("clamp01")),
            Is.True);
        Assert.That(plan.Findings.Any(f => f.Message.Contains("reserved for the framework")), Is.True);
        Assert.That(plan.Survivors, Has.Count.EqualTo(1));
    }

    [Test]
    public void ShouldWarnOnATypoedMmapiCallAndStillApply()
    {
        var mod = SyntheticLayer.Mod("testmod",
            "function testmod_boot() {\n"
            + "    mmapi_emit(\"game.step_begin\", undefined);\n"
            + "    mmapi_emitt(\"game.step_begin\", undefined);\n"
            + "}\n");

        var plan = Stage(null, null, mod);

        var hits = plan.Findings.Where(f => f.Message.Contains("mmapi_emitt")).ToList();
        Assert.That(hits, Has.Count.EqualTo(1));
        Assert.That((hits[0].File, hits[0].Line), Is.EqualTo(("gml/core/State.gml", 3)));
        Assert.That(plan.Survivors, Has.Count.EqualTo(1));
    }

    [Test]
    public void ShouldExcludeATypoedMmapiCallUnderStrictLints()
    {
        var mod = SyntheticLayer.Mod("testmod", "function testmod_boot() {\n    mmapi_emitt(1);\n}\n");

        var plan = Stage(new GmlLayerOptions { StrictLints = true }, null, mod);

        Assert.That(plan.Excluded.Select(e => e.Mod.Id), Is.EqualTo(new[] { "testmod" }));
    }

    [Test]
    public void ShouldReportGlobalClobberAndForeignRootFindings()
    {
        var alpha = SyntheticLayer.Mod("alpha",
            "function alpha_boot() {\n"
            + "    global.shared = 1;\n"
            + "    global.__beta = {};\n"
            + "    global.__beta.flag = true;\n"
            + "}\n");
        var beta = SyntheticLayer.Mod("beta", "function beta_boot() {\n    global.shared = 2;\n}\n");

        var plan = Stage(null, null, alpha, beta);

        Assert.That(plan.Findings.Any(f => f.Message.Contains("mod 'alpha' also writes")
                                           && f.Message.Contains("global.shared")), Is.True);
        Assert.That(plan.Findings.Any(f => f.Message.Contains("namespace root")
                                           && f.Message.Contains("__beta")), Is.True);
    }

    [Test]
    public void ShouldRunTheSharedSetAsFilesAndEachModAsAUnit()
    {
        var gate = new ScriptedGate();

        Stage(null, gate);

        Assert.That(gate.Calls.Select(c => c.Mode), Is.EqualTo(new[] { "files", "unit" }));
        var shared = gate.Calls[0].Paths.Select(p => p.Replace('\\', '/')).ToList();
        Assert.That(shared.Any(p => p.Contains("/scripts/mmapi/")), Is.True);
        Assert.That(shared.Any(p => p.Contains("/objects/")), Is.True);
        var unit = gate.Calls[1].Paths.Select(p => p.Replace('\\', '/')).ToList();
        Assert.That(unit, Is.All.Contains("/scripts/testmod/"));
    }

    [Test]
    public void ShouldAbortWhenTheSharedSetFailsToCompile()
    {
        var gate = new ScriptedGate { Fails = (_, _) => "framework.gml: parse error" };

        Assert.Throws<InvalidOperationException>(() => Stage(null, gate));
    }

    private static ScriptedGate SharedPassesModsFail() => new()
    {
        Fails = (_, paths) =>
        {
            var normalised = paths.Select(p => p.Replace('\\', '/')).ToList();
            if (normalised.Any(p => p.Contains("/scripts/mmapi/") || p.Contains("/objects/"))) return null;
            return "bad.gml";
        },
    };

    [Test]
    public void ShouldSkipAModThatFailsToCompile()
    {
        var plan = Stage(null, SharedPassesModsFail());

        Assert.That(plan.Excluded.Select(e => e.Mod.Id), Is.EqualTo(new[] { "testmod" }));
        Assert.That(plan.Excluded[0].Reasons[0], Does.Contain("does not compile"));
        Assert.That(plan.Added.Keys, Has.None.StartWith("assets/gml/scripts/testmod/"));
        Assert.That(plan.Added, Contains.Key("assets/gml/scripts/mmapi/mmapi.gml"));
    }

    [Test]
    public void ShouldAbortOnACompileFailureUnderFailOnSkip()
    {
        var exception = Assert.Throws<InvalidOperationException>(() =>
            Stage(new GmlLayerOptions { FailOnSkip = true }, SharedPassesModsFail()));

        Assert.That(exception!.Message, Does.Contain("fail-on-skip"));
    }

    [Test]
    public void ShouldStageWithoutAGate()
    {
        var plan = Stage();

        Assert.That(plan.Excluded, Is.Empty);
        Assert.That(plan.Added, Contains.Key("assets/gml/scripts/testmod/core/State.gml"));
    }

    [Test]
    public void ShouldDeliverTheMmapiFrameworkByteExact()
    {
        var plan = Stage();

        var sources = PayloadResolver.MmapiSources();
        Assert.That(sources, Has.Count.EqualTo(8));

        var delivered = plan.Added
            .Where(e => e.Key.StartsWith(SeamStager.MmapiTreePrefix, StringComparison.Ordinal))
            .Where(e => e.Key != SeamStager.HookCatalogRel)
            .ToDictionary(e => e.Key, e => e.Value);
        Assert.That(delivered.Keys,
            Is.EquivalentTo(sources.Select(s => SeamStager.MmapiTreePrefix + s.Name)));
        foreach (var (name, bytes) in sources)
            Assert.That(delivered[SeamStager.MmapiTreePrefix + name], Is.EqualTo(bytes),
                $"{name}: delivered bytes differ from the embedded payload");

        Assert.That(plan.Added, Contains.Key(SeamStager.HookCatalogRel));
    }

    [Test]
    public void ShouldProduceIdenticalPlansAcrossTwoStages()
    {
        var first = Stage();
        var second = Stage();

        Assert.That(second.Added.Keys, Is.EquivalentTo(first.Added.Keys));
        foreach (var (rel, bytes) in second.Added)
            Assert.That(bytes, Is.EqualTo(first.Added[rel]), $"{rel}: bytes differ between stages");
        Assert.That(second.Seamed.Keys, Is.EquivalentTo(first.Seamed.Keys));
        foreach (var (rel, staged) in second.Seamed)
            Assert.That(staged.Text, Is.EqualTo(first.Seamed[rel].Text), $"{rel}: text differs between stages");
    }

    // The slice gate: a full offline apply onto a synthesised store lands
    // mmapi, seams and mod gml; a re-apply converges byte-identically; a
    // corrupted live archive heals.
    [Test]
    public void ShouldApplyTheLayerOntoTheStoreAndConverge()
    {
        var fom = Path.Combine(Path.GetTempPath(), "momi_layer_" + Path.GetRandomFileName());
        Directory.CreateDirectory(fom);
        try
        {
            var livePath = Path.Combine(fom, "assets.zip");
            using (var archive = ZipFile.Open(livePath, ZipArchiveMode.Create))
            {
                WriteEntry(archive, "assets/gml/objects/Game.gml", SyntheticLayer.PristineGame);
                WriteEntry(archive, "assets/gml/objects/Other.gml", SyntheticLayer.PristineOther);
                WriteEntry(archive, "assets/data/items.json", "{}");
            }

            var first = ApplyOnce(fom);

            using (var live = ZipFile.OpenRead(livePath))
            {
                Assert.That(ReadEntry(live, "assets/gml/objects/Game.gml"), Does.Contain("__momi_test_game_step"));
                Assert.That(ReadEntry(live, "assets/gml/objects/Other.gml"), Does.Contain("__momi_test_other_fix"));
                Assert.That(live.GetEntry("assets/gml/scripts/mmapi/mmapi.gml"), Is.Not.Null);
                Assert.That(live.GetEntry(SeamStager.HookCatalogRel), Is.Not.Null);
                Assert.That(live.GetEntry("assets/gml/scripts/testmod/core/State.gml"), Is.Not.Null);
                Assert.That(live.GetEntry("assets/data/items.json"), Is.Not.Null);
                Assert.That(live.GetEntry("manifest.toml"), Is.Not.Null);
            }

            var second = ApplyOnce(fom);
            Assert.That(second, Is.EqualTo(first));

            File.WriteAllBytes(livePath, Encoding.UTF8.GetBytes("CORRUPT GARBAGE"));
            var healed = ApplyOnce(fom);
            Assert.That(healed, Is.EqualTo(first));
        }
        finally
        {
            Directory.Delete(fom, true);
        }
    }

    private static Dictionary<string, string> ApplyOnce(string fom)
    {
        var store = new AssetsStore(fom);
        store.EnsureBackup();

        GmlLayerPlan plan;
        using (var pristine = new ZipPristineSource(store.BackupPath))
        {
            plan = GmlLayer.Stage(SyntheticLayer.Catalog(), pristine, [SyntheticLayer.Mod("testmod")], null);
        }

        var modifier = store.BeginRebuild();
        modifier.Write("manifest.toml", "");
        foreach (var (rel, bytes) in plan.Added) modifier.Write(rel, bytes);
        foreach (var (rel, staged) in plan.Seamed) modifier.Write(rel, staged.Encode());
        store.Commit();

        using var live = ZipFile.OpenRead(store.LivePath);
        return live.Entries.ToDictionary(e => e.FullName, e =>
        {
            using var stream = e.Open();
            using var buffer = new MemoryStream();
            stream.CopyTo(buffer);
            return Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(buffer.ToArray()));
        });
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
