using Garethp.ModsOfMistriaInstallerLib.GmlMods;
using Garethp.ModsOfMistriaInstallerLib.Seam;
using ModsOfMistriaInstallerLibTests.Fixtures;

namespace ModsOfMistriaInstallerLibTests.GmlMods;

[TestFixture]
public class GmlModLintTest
{
    // What the post-apply tree exports: the framework plus the generated hook
    // catalog, exactly as the skip pass computes it
    private static readonly Dictionary<string, string> Exports = new()
    {
        { "mmapi_emit", "the mmapi framework (mmapi_hooks.gml)" },
        { "mmapi_on", "the mmapi framework (mmapi_hooks.gml)" },
        { "mmapi_hook_catalog", "the mmapi framework (mmapi_hook_catalog.gml)" },
        { "__mmapi_guarded_call", "the mmapi framework (mmapi.gml)" },
        { "scr_engine_thing", "the engine (assets/gml/objects/Game.gml)" },
    };

    private static readonly SeamCatalog ContentionCatalog = new(2, [],
    [
        new HookDeclaration("obj.take", HookKind.Override, "d", HookProvider.Seam, [], false,
            HookContention.ClaimScoped),
        new HookDeclaration("calc.max", HookKind.Override, "d", HookProvider.Seam, [], false,
            HookContention.Exclusive),
    ], []);

    private static GmlModCode Mod(string id, string text, string? dirName = null)
    {
        var mock = new MockMod(new Dictionary<string, string> { { "gml/Main.gml", text } })
        {
            Id = id,
            DirName = dirName ?? id,
        };
        return GmlModCollector.Collect(mock)!;
    }

    private static List<LintFinding> LintSymbols(params GmlModCode[] mods) =>
        GmlModLint.LintSymbols(mods, mods.ToDictionary(m => m.Id, GmlModLint.ScanSymbols));

    private static List<LintFinding> LintCalls(params GmlModCode[] mods) =>
        GmlModLint.LintMmapiCalls(mods, mods.ToDictionary(m => m.Id, GmlModLint.ScanSymbols), Exports);

    private static List<LintFinding> CrossModFindings(params GmlModCode[] mods) =>
        GmlModLint.LintHooks(mods, ContentionCatalog).Where(f => f.File.Length == 0).ToList();

    [Test]
    public void ShouldStaySilentForASingleOverrider()
    {
        var cross = CrossModFindings(Mod("a", "mmapi_override(\"obj.take\", a_fn);\n"));

        Assert.That(cross, Is.Empty);
    }

    [Test]
    public void ShouldReportCoexistenceForClaimScopedContention()
    {
        var cross = CrossModFindings(
            Mod("a", "mmapi_override(\"obj.take\", a_fn);\n"),
            Mod("b", "mmapi_override(\"obj.take\", b_fn);\n"));

        Assert.That(cross.Select(f => f.ModId), Is.EquivalentTo(new[] { "a", "b" }));
        Assert.That(cross[0].Message, Does.Contain("claim-scoped"));
        Assert.That(cross[0].Message, Does.Contain("coexist"));
    }

    [Test]
    public void ShouldReportTheConflictForExclusiveContention()
    {
        var cross = CrossModFindings(
            Mod("a", "mmapi_override(\"calc.max\", a_fn);\n"),
            Mod("b", "mmapi_override(\"calc.max\", b_fn);\n"));

        Assert.That(cross.Select(f => f.ModId), Is.EquivalentTo(new[] { "a", "b" }));
        Assert.That(cross[0].Message, Does.Contain("exclusive"));
        Assert.That(cross[0].Message, Does.Contain("never take effect"));
    }

    [Test]
    public void ShouldReportATypoedMmapiCall()
    {
        var findings = LintCalls(Mod("a", "function a_boot() {\n"
                                          + "    mmapi_emitt(\"x.y\", undefined);\n}\n"));

        Assert.That(findings, Has.Count.EqualTo(1));
        Assert.That(findings[0].Message, Does.Contain("mmapi_emitt"));
        Assert.That(findings[0].Message, Does.Contain("typo"));
        Assert.That((findings[0].File, findings[0].Line), Is.EqualTo(("gml/Main.gml", 2)));
    }

    [Test]
    public void ShouldStaySilentForRealFrameworkCalls()
    {
        var findings = LintCalls(Mod("a", "function a_boot() {\n"
                                          + "    mmapi_emit(\"x.y\", undefined);\n"
                                          + "    mmapi_on(\"x.y\", a_h);\n"
                                          + "    __mmapi_guarded_call();\n}\n"));

        Assert.That(findings, Is.Empty);
    }

    [Test]
    public void ShouldCountTheGeneratedHookCatalogAsDefined()
    {
        Assert.That(LintCalls(Mod("a", "var c = mmapi_hook_catalog();\n")), Is.Empty);
    }

    [Test]
    public void ShouldLetAnMmapiPrefixedModCallItsOwnExports()
    {
        var mod = Mod("mmapi_tools", "function mmapi_tools_boot() {\n"
                                     + "    mmapi_tools_helper();\n}\n"
                                     + "function mmapi_tools_helper() {\n}\n");

        Assert.That(LintCalls(mod), Is.Empty);
    }

    [Test]
    public void ShouldLetOneModCallAnotherModsExport()
    {
        var provider = Mod("mmapi_tools", "function mmapi_tools_helper() {\n}\n");
        var consumer = Mod("b", "function b_boot() {\n    mmapi_tools_helper();\n}\n");

        Assert.That(LintCalls(provider, consumer), Is.Empty);
    }

    [Test]
    public void ShouldNeverMatchStringsCommentsOrMemberCalls()
    {
        var mod = Mod("a", "function a_boot() {\n"
                           + "    // mmapi_ghost_one() lives in a comment\n"
                           + "    var s = \"mmapi_ghost_two()\";\n"
                           + "    obj.mmapi_ghost_three();\n"
                           + "    var v = mmapi_ghost_four;\n"
                           + "}\n");

        Assert.That(LintCalls(mod), Is.Empty);
    }

    [Test]
    public void ShouldExemptAWrappedOriginalsCall()
    {
        Assert.That(LintCalls(Mod("a", "var r = __mmapi_orig_draw_self();\n")), Is.Empty);
    }

    [Test]
    public void ShouldLeaveNonMmapiUnknownsAlone()
    {
        Assert.That(LintCalls(Mod("a", "some_engine_thing_we_cannot_know();\n")), Is.Empty);
    }

    [Test]
    public void ShouldCollectTopLevelFunctionsAndGlobalRoots()
    {
        var mod = Mod("alpha", "function alpha_boot() {\n"
                               + "    global.__alpha = {};\n"
                               + "    global.__alpha.count = 0;\n"
                               + "    var nested = function() {};\n"
                               + "}\n"
                               + "function alpha_tick() {\n}\n");

        var symbols = GmlModLint.ScanSymbols(mod);

        Assert.That(symbols.Functions.Keys.Order(), Is.EqualTo(new[] { "alpha_boot", "alpha_tick" }));
        Assert.That(symbols.Functions["alpha_boot"], Is.EqualTo(("gml/Main.gml", 1)));
        Assert.That(symbols.GlobalRoots.Keys, Is.EquivalentTo(new[] { "__alpha" }));
        Assert.That(symbols.GlobalRoots["__alpha"].Bare, Is.True);
    }

    [Test]
    public void ShouldRecordADeepOnlyRootAsNonBare()
    {
        var symbols = GmlModLint.ScanSymbols(Mod("alpha", "global.__other.flag = true;\n"));

        Assert.That(symbols.GlobalRoots["__other"].Bare, Is.False);
    }

    [Test]
    public void ShouldRecordIntraModDuplicates()
    {
        var mock = new MockMod(new Dictionary<string, string>
        {
            { "gml/A.gml", "function alpha_util() {\n}\n" },
            { "gml/B.gml", "function alpha_util() {\n}\n" },
        }) { Id = "alpha", DirName = "alpha" };
        var mod = GmlModCollector.Collect(mock)!;

        var symbols = GmlModLint.ScanSymbols(mod);

        Assert.That(symbols.Functions["alpha_util"], Is.EqualTo(("gml/A.gml", 1)));
        Assert.That(symbols.Duplicates.Keys, Is.EquivalentTo(new[] { "alpha_util" }));
        Assert.That(symbols.Duplicates["alpha_util"], Is.EqualTo(new[] { ("gml/B.gml", 1) }));
    }

    [Test]
    public void ShouldPassPrefixedFunctions()
    {
        // dir-name and manifest-symbol prefixes both count, __ variants included
        // (the shape: mod dir "mock_mod" differing from its id "mm")
        var mod = Mod("mm", "function mm_probe() {\n}\n"
                            + "function mock_mod_boot() {\n}\n"
                            + "function __mock_mod_util() {\n}\n",
            dirName: "mock_mod");

        Assert.That(LintSymbols(mod), Is.Empty);
    }

    [Test]
    public void ShouldWarnOnAnUnprefixedFunction()
    {
        var findings = LintSymbols(Mod("alpha", "function clamp01(x) {\n}\n"));

        Assert.That(findings, Has.Count.EqualTo(1));
        Assert.That(findings[0].Message, Does.Contain("not namespaced"));
        Assert.That(findings[0].Message, Does.Contain("clamp01"));
        Assert.That(findings[0].File, Is.EqualTo("gml/Main.gml"));
    }

    [Test]
    public void ShouldWarnOnAReservedMmapiGlobalEvenDeep()
    {
        var findings = LintSymbols(Mod("alpha", "global.__mmapi_hooks.x = 1;\n"));

        Assert.That(findings, Has.Count.EqualTo(1));
        Assert.That(findings[0].Message, Does.Contain("reserved for the framework"));
    }

    [Test]
    public void ShouldExemptTheOwnRootEvenWithAPrefixSiblingMod()
    {
        var beta = Mod("beta", "function beta_boot() {\n    global.__beta = {};\n}\n");
        var betaFx = Mod("beta_fx", "function beta_fx_boot() {\n    global.__beta_fx = {};\n}\n");
        Assert.That(LintSymbols(beta, betaFx), Is.Empty);

        var tools = Mod("mmapi_tools", "function mmapi_tools_boot() {\n    global.__mmapi_tools = {};\n}\n");
        Assert.That(LintSymbols(tools), Is.Empty);
    }

    [Test]
    public void ShouldWarnOnReplacingAForeignNamespaceRoot()
    {
        var alpha = Mod("alpha", "function alpha_boot() {\n    global.__beta = {};\n}\n");
        var beta = Mod("beta", "function beta_boot() {\n    global.__beta = {};\n}\n");

        var foreign = LintSymbols(alpha, beta).Where(f => f.Message.Contains("namespace root")).ToList();

        Assert.That(foreign, Has.Count.EqualTo(1));
        Assert.That(foreign[0].ModId, Is.EqualTo("alpha"));
    }

    [Test]
    public void ShouldSanctionDeepWritesIntoAForeignRoot()
    {
        var alpha = Mod("alpha", "global.__beta.skip_next_dig = true;\n");
        var beta = Mod("beta", "function beta_boot() {\n}\n");

        Assert.That(LintSymbols(alpha, beta), Is.Empty);
    }

    [Test]
    public void ShouldWarnTheLaterModReplacingASharedRoot()
    {
        var alpha = Mod("alpha", "function alpha_boot() {\n    global.shared = 1;\n}\n");
        var beta = Mod("beta", "function beta_boot() {\n    global.shared = 2;\n}\n");

        var clobber = LintSymbols(alpha, beta).Where(f => f.Message.Contains("also writes")).ToList();

        Assert.That(clobber, Has.Count.EqualTo(1));
        Assert.That(clobber[0].ModId, Is.EqualTo("beta"));
        Assert.That(clobber[0].Message, Does.Contain("'alpha'"));
    }
}
