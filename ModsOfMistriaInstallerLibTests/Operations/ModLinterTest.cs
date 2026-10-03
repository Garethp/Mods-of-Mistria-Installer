using System.Text;
using Garethp.ModsOfMistriaInstallerLib.Generator;
using Garethp.ModsOfMistriaInstallerLib.GmlMods;
using Garethp.ModsOfMistriaInstallerLib.Operations;
using Garethp.ModsOfMistriaInstallerLib.Seam;
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
        new(new Dictionary<string, object> { { "images/icon.png", "" } })
        {
            Id = "mod.a",
            Version = "0.0.1",
        };

    private static ModLintResult Lint(MockMod mod, GmlLayerOptions? options = null,
        ScriptedGate? gate = null) =>
        ModLinter.Lint(mod, SyntheticLayer.Pristine(), gate, options, SyntheticLayer.Catalog());

    private static MockMod GmlMod(string gml, List<string>? requiresHooks = null) =>
        new(new Dictionary<string, object> { { "gml/core/State.gml", gml } })
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
        Assert.That(result.ExclusionReasons, Has.Some.Contains("Compile Error"));
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

    // Extension registrations lint the way they install: the collector's
    // problems exclude the mod, the survivors stage so the generated registry
    // resolves, and a mod of registrations alone still enters the layer.

    private const string CatalogWithPoint = SyntheticLayer.CatalogToml + "\n" + """

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
        """ + "\n";

    private const string OtherWithEnum =
        "enum Thing {\n    Alpha,\n    LEN\n}\n\n" + SyntheticLayer.PristineOther;

    private static ModLintResult LintWithPoint(MockMod mod, GmlLayerOptions? options = null) =>
        ModLinter.Lint(mod, SyntheticLayer.Pristine(other: OtherWithEnum), null, options,
            SeamCatalogLoader.Load(Encoding.UTF8.GetBytes(CatalogWithPoint), "synthetic"));

    private static MockMod RegisteringMod(string registrationFile, string? gml = null)
    {
        var files = new Dictionary<string, object> { { registrationFile, "object = \"obj_x\"\n" } };
        if (gml is not null) files["gml/core/State.gml"] = gml;
        return new MockMod(files) { Id = "mod.a", DirName = "mod_a", Version = "0.0.1" };
    }

    [Test]
    public void ShouldStageARegistrationSoTheGeneratedRegistryResolves()
    {
        var mod = RegisteringMod("momi/extensions/roster/luna.toml",
            "function mod_a_boot() {\n    return mmapi_ext_id(\"roster\", \"luna\");\n}\n");

        var result = LintWithPoint(mod, new GmlLayerOptions { StrictLints = true });

        Assert.That(result.Ok, Is.True, string.Join("; ", result.ExclusionReasons));
        Assert.That(result.RegistrationCount, Is.EqualTo(1));
        Assert.That(result.Findings.Select(f => f.ToString()), Has.None.Contains("mmapi_ext_id"));
        Assert.That(ModLinter.RenderText(result, "mod_a"), Does.Contain("registrations: 1 staged"));
    }

    [Test]
    public void ShouldExcludeAModWhoseRegistrationHasAProblem()
    {
        var result = LintWithPoint(RegisteringMod("momi/extensions/roster/Luna.toml"));

        Assert.That(result.Ok, Is.False);
        Assert.That(result.ExclusionReasons, Has.Count.EqualTo(1));
        Assert.That(result.ExitCode, Is.EqualTo(1));
        Assert.That(result.Gate, Is.EqualTo(LintGateOutcome.NotReached));
        var text = ModLinter.RenderText(result, "mod_a");
        Assert.That(text, Does.Contain("EXCLUDED"));
        Assert.That(text, Does.Contain("not staged, the registration problems below exclude the mod"));
        Assert.That(text, Does.Not.Contain("no checker backend"));
    }

    [Test]
    public void ShouldStageAModOfRegistrationsAlone()
    {
        var result = LintWithPoint(RegisteringMod("momi/extensions/roster/luna.toml"));

        Assert.That(result.Ok, Is.True, string.Join("; ", result.ExclusionReasons));
        Assert.That(result.Symbol, Is.Not.Null);
        Assert.That(result.GmlFileCount, Is.EqualTo(0));
        Assert.That(result.RegistrationCount, Is.EqualTo(1));
        Assert.That(ModLinter.RenderText(result, "mod_a"), Does.Not.Contain("manifest checks only"));
    }
}
