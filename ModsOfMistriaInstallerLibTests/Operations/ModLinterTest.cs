using Garethp.ModsOfMistriaInstallerLib.Generator;
using Garethp.ModsOfMistriaInstallerLib.GmlMods;
using Garethp.ModsOfMistriaInstallerLib.Operations;
using ModsOfMistriaInstallerLibTests.Fixtures;
using ModsOfMistriaInstallerLibTests.TestUtils;

namespace ModsOfMistriaInstallerLibTests.Operations;

// Read-only mod lint over the synthetic layer: the result and exit-code
// contract, not the checks themselves - the skip pass, the lints and the gate
// have their own suites, and lint reuses them through GmlLayer.Stage.
[TestFixture]
public class ModLinterTest
{
    private static MockMod ContentOnlyMod() =>
        new(new Dictionary<string, string> { { "images/icon.png", "" } })
        {
            Id = "mod.a",
            Version = "0.0.1",
        };

    private static ModLintResult Lint(MockMod mod, GmlLayerOptions? options = null,
        ScriptedGate? gate = null) =>
        ModLinter.Lint(mod, SyntheticLayer.Pristine(), gate, options, SyntheticLayer.Catalog());

    private static MockMod GmlMod(string gml, List<string>? requiresHooks = null) =>
        new(new Dictionary<string, string> { { "gml/core/State.gml", gml } })
        {
            Id = "mod.a",
            DirName = "mod_a",
            Version = "0.0.1",
            RequiredHooks = requiresHooks ?? [],
        };

    [Test]
    public void ShouldPassACleanMod()
    {
        var result = Lint(GmlMod("function mod_a_boot() {\n}\n"), gate: new ScriptedGate());

        Assert.That(result.Ok, Is.True);
        Assert.That(result.Symbol, Is.EqualTo("mod_a"));
        Assert.That(result.GmlFileCount, Is.EqualTo(1));
        Assert.That(result.GateRan, Is.True);
        Assert.That(result.Findings, Is.Empty);
        Assert.That(result.ExclusionReasons, Is.Empty);
        Assert.That(ModLinter.RenderText(result, "mod_a"), Does.Contain("RESULT: OK"));
    }

    [Test]
    public void ShouldReportFindingsWithoutFailing()
    {
        // warn tier is the apply's: an unprefixed function is a finding, not
        // an exclusion
        var result = Lint(GmlMod("function unprefixed() {\n}\n"));

        Assert.That(result.Ok, Is.True);
        Assert.That(result.ExitCode, Is.EqualTo(0));
        Assert.That(result.Findings.Select(f => f.Message), Has.Some.Contains("not namespaced"));
        Assert.That(ModLinter.RenderText(result, "mod_a"), Does.Contain("(with warnings)"));
    }

    [Test]
    public void ShouldEscalateFindingsUnderStrictLints()
    {
        var result = Lint(GmlMod("function unprefixed() {\n}\n"),
            new GmlLayerOptions { StrictLints = true });

        Assert.That(result.Ok, Is.False);
        Assert.That(result.ExitCode, Is.EqualTo(1));
        Assert.That(result.ExclusionReasons, Has.Some.Contains("strict-lints"));
    }

    [Test]
    public void ShouldFailWhenARequiredHookIsMissing()
    {
        var result = Lint(GmlMod("function mod_a_boot() {\n}\n", ["absent.hook"]));

        Assert.That(result.Ok, Is.False);
        Assert.That(result.ExclusionReasons, Has.Some.Contains("absent.hook"));
        Assert.That(ModLinter.RenderText(result, "mod_a"), Does.Contain("RESULT: FAIL"));
    }

    [Test]
    public void ShouldFailWhenTheModDoesNotCompile()
    {
        var gate = new ScriptedGate
        {
            Fails = (mode, _) => mode == "unit" ? "bad.gml: parse error" : null,
        };

        var result = Lint(GmlMod("function mod_a_boot() {\n}\n"), gate: gate);

        Assert.That(result.Ok, Is.False);
        Assert.That(result.ExclusionReasons, Has.Some.Contains("does not compile"));
    }

    [Test]
    public void ShouldFailOnManifestErrors()
    {
        // the install flow skips a mod with validation errors before its GML
        // is read; lint reports the same fate
        var mod = GmlMod("function mod_a_boot() {\n}\n");
        mod.GetValidation().Errors.Add(new ValidationMessage(mod, "manifest.json", "no author"));

        var result = Lint(mod);

        Assert.That(result.Ok, Is.False);
        Assert.That(result.ExitCode, Is.EqualTo(1));
        Assert.That(result.ManifestErrors, Has.Some.Contains("no author"));
        Assert.That(ModLinter.RenderText(result, "mod_a"), Does.Contain("manifest ERROR"));
    }

    [Test]
    public void ShouldLintTheManifestAloneForAContentOnlyMod()
    {
        var result = Lint(ContentOnlyMod());

        Assert.That(result.Ok, Is.True);
        Assert.That(result.Symbol, Is.Null);
        Assert.That(ModLinter.RenderText(result, "mod_a"), Does.Contain("manifest checks only"));
    }

    [Test]
    public void ShouldPinTheExitCodes()
    {
        var clean = Lint(GmlMod("function mod_a_boot() {\n}\n"));
        Assert.That(clean.ExitCode, Is.EqualTo(0));

        var excluded = Lint(GmlMod("function mod_a_boot() {\n}\n", ["absent.hook"]));
        Assert.That(excluded.ExitCode, Is.EqualTo(1));
    }
}
