using System.Text;
using Garethp.ModsOfMistriaInstallerLib.GmlMods;
using Garethp.ModsOfMistriaInstallerLib.Seam;
using ModsOfMistriaInstallerLibTests.Fixtures;

namespace ModsOfMistriaInstallerLibTests.GmlMods;

// A registration is data. These pin what a mod may say, what it may not,
// and that nothing it says lands as code.
[TestFixture]
public class ExtensionCollectorTest
{
    private const string Catalog = """
        version = 2

        [[extension]]
        id   = "roster"
        file = "gml/NpcId.gml"

        [extension.ordinal]
        enum     = "NpcId"
        sentinel = "LEN"

        [[extension.fields]]
        name = "object"
        type = "identifier"
        doc  = "GML object name."

        [[extension.fields]]
        name = "label"
        type = "string"
        doc  = "Display label."

        [[extension.sites]]
        id       = "enum_member"
        kind     = "enum_member"
        template = "{{symbol}} = {{ordinal}},"
        indent   = 4

        [extension.vacancy]
        enum_member = "{{symbol}} = {{ordinal}},"
        """ + "\n";

    // the same point plus the mandatory-data companion npc_roster will carry, and
    // the vacancy_files block satisfies the error-companion coupling rule
    private const string WithCompanion = Catalog + "\n" + """
        [[extension.companions]]
        path  = "fiddle/npcs/{{symbol}}.toml"
        level = "error"
        doc   = "The NPC prototype. Absent, the game crashes during Setup."

        [[extension.companions]]
        path  = "gml/{{symbol}}_object.gml"
        level = "warning"
        doc   = "The object this NPC uses."

        [[extension.vacancy_files]]
        path    = "fiddle/npcs/{{symbol}}.toml"
        content = "name = \"Departed\"\n"
        """ + "\n";

    private static SeamCatalog Load(string? text = null) =>
        SeamCatalogLoader.Load(Encoding.UTF8.GetBytes(text ?? Catalog), "seams.toml");

    private static ExtensionCollection Collect(Dictionary<string, string> files, string id = "author.mod",
        string? catalogText = null) =>
        ExtensionCollector.Collect(new MockMod(files.ToDictionary(kv => kv.Key, kv => (object)kv.Value)) { Id = id },
            Load(catalogText));

    private static Dictionary<string, string> OneRegistration(string body) => new()
    {
        ["momi/extensions/roster/luna.toml"] = body,
    };

    [Test]
    public void ShouldCollectAValidRegistration()
    {
        var result = Collect(OneRegistration("object = \"author_mod_luna_obj\"\nlabel = \"Luna\"\n"));

        Assert.That(result.Problems, Is.Empty);
        var registration = result.Registrations.Single();
        Assert.That(registration.PointId, Is.EqualTo("roster"));
        Assert.That(registration.LocalName, Is.EqualTo("luna"));
        Assert.That(registration.ModId, Is.EqualTo("author.mod"));
        Assert.That(registration.RenderedValues["object"], Is.EqualTo("author_mod_luna_obj"));
        Assert.That(registration.RenderedValues["label"], Is.EqualTo("\"Luna\""));
    }

    [Test]
    public void ShouldPrefixTheSymbolWithTheModSymbol()
    {
        // the prefix is what lets two mods both ship a "luna". It does not
        // make collisions impossible (differently split prefixes can compose
        // the same symbol), which is why the expander carries its own guard
        var result = Collect(OneRegistration("object = \"o\"\nlabel = \"L\"\n"), "author.my-mod");

        Assert.That(result.Registrations.Single().Symbol, Is.EqualTo("author_my_mod_luna"));
    }

    [Test]
    public void ShouldRejectAPrefixStartingWithADigit()
    {
        // a leading-digit prefix composes an invalid GML enum member, and
        // nothing downstream would reject it cleanly
        var result = Collect(OneRegistration("object = \"o\"\nlabel = \"L\"\n"), "8bitfan.mod");

        Assert.That(result.Registrations, Is.Empty);
        Assert.That(result.Problems.Single(), Does.Contain("symbol prefix"));
    }

    [Test]
    public void ShouldRejectAPrefixStartingWithAnUnderscore()
    {
        // stripped punctuation in an author name can leave a leading
        // underscore, which installs as valid GML but composes a symbol the
        // reseed harvesters can never recover, a silent recovery hole
        var result = Collect(OneRegistration("object = \"o\"\nlabel = \"L\"\n"), "_gareth.mod");

        Assert.That(result.Registrations, Is.Empty);
        Assert.That(result.Problems.Single(), Does.Contain("symbol prefix"));
    }

    [Test]
    public void ShouldRejectAComposedSymbolOverTheSharedLengthCap()
    {
        // prefix and local name are each in shape, but together they exceed
        // the 81-char recoverability cap the harvesters enforce
        var longId = new string('a', 20) + "." + new string('b', 19);   // prefix 40 chars
        var longLocal = "c" + new string('d', 40);                      // local 41 chars
        var result = Collect(new Dictionary<string, string>
        {
            [$"momi/extensions/roster/{longLocal}.toml"] = "object = \"o\"\nlabel = \"L\"\n",
        }, longId);

        Assert.That(result.Registrations, Is.Empty);
        Assert.That(result.Problems.Single(), Does.Contain("81-char"));
    }

    [Test]
    public void ShouldReturnNothingForAModWithNoRegistrations()
    {
        var result = Collect(new Dictionary<string, string> { ["gml/S.gml"] = "// x\n" });

        Assert.That(result.Registrations, Is.Empty);
        Assert.That(result.Problems, Is.Empty);
    }

    [Test]
    public void ShouldRejectAnIdentifierThatIsNotOne()
    {
        // the injection case. An identifier lands as a bare token, so its
        // charset is the thing standing between a registration and arbitrary
        // engine code
        var result = Collect(OneRegistration("object = \"obj_x; halt()\"\nlabel = \"L\"\n"));

        Assert.That(result.Registrations, Is.Empty);
        Assert.That(result.Problems.Single(), Does.Contain("is not a GML identifier"));
    }

    [Test]
    public void ShouldEscapeAStringRatherThanInterpolateIt()
    {
        var result = Collect(OneRegistration(
            "object = \"o\"\nlabel = \"say \\\"hi\\\" \\\\ done\"\n"));

        Assert.That(result.Problems, Is.Empty);
        Assert.That(result.Registrations.Single().RenderedValues["label"],
            Is.EqualTo("\"say \\\"hi\\\" \\\\ done\""));
    }

    [Test]
    public void ShouldRejectAMissingField()
    {
        var result = Collect(OneRegistration("object = \"o\"\n"));

        Assert.That(result.Problems.Single(), Does.Contain("field 'label' is missing"));
    }

    [Test]
    public void ShouldRejectAnUnknownField()
    {
        // a typo'd field would otherwise read as a missing one, and the
        // registrant would install with a value it never asked for
        var result = Collect(OneRegistration("object = \"o\"\nlabel = \"L\"\nobjekt = \"o\"\n"));

        Assert.That(result.Problems.Single(), Does.Contain("unknown field 'objekt'"));
        Assert.That(result.Problems.Single(), Does.Contain("object (identifier), label (string)"));
    }

    [Test]
    public void ShouldRejectATypeMismatch()
    {
        var result = Collect(OneRegistration("object = 42\nlabel = \"L\"\n"));

        Assert.That(result.Problems.Single(), Does.Contain("must be a string naming a GML identifier"));
    }

    [Test]
    public void ShouldRejectAnUnknownPoint()
    {
        var result = Collect(new Dictionary<string, string>
        {
            ["momi/extensions/spell_roster/fire.toml"] = "object = \"o\"\n",
        });

        Assert.That(result.Problems.Single(), Does.Contain("unknown extension point 'spell_roster'"));
        Assert.That(result.Problems.Single(), Does.Contain("newer MOMI"));
    }

    [Test]
    public void ShouldRejectABadLocalName()
    {
        var result = Collect(new Dictionary<string, string>
        {
            ["momi/extensions/roster/Luna.toml"] = "object = \"o\"\nlabel = \"L\"\n",
        });

        Assert.That(result.Problems.Single(), Does.Contain("must match"));
    }

    [Test]
    public void ShouldRejectARegistrationNotOneLevelUnderThePoint()
    {
        var result = Collect(new Dictionary<string, string>
        {
            ["momi/extensions/roster/deep/luna.toml"] = "object = \"o\"\n",
        });

        Assert.That(result.Problems.Single(), Does.Contain("is not at momi/extensions/<point>/<name>.toml"));
    }

    [Test]
    public void ShouldCollectSeveralRegistrationsSortedBySymbol()
    {
        var result = Collect(new Dictionary<string, string>
        {
            ["momi/extensions/roster/wren.toml"] = "object = \"o1\"\nlabel = \"W\"\n",
            ["momi/extensions/roster/luna.toml"] = "object = \"o2\"\nlabel = \"L\"\n",
        });

        Assert.That(result.Problems, Is.Empty);
        Assert.That(result.Registrations.Select(r => r.LocalName), Is.EqualTo(new[] { "luna", "wren" }));
    }

    [Test]
    public void ShouldExcludeTheModWhenAnErrorLevelCompanionIsMissing()
    {
        // not a degraded NPC but a crash during Setup, so excluding the mod beats
        // shipping a game that will not boot
        var result = Collect(OneRegistration("object = \"o\"\nlabel = \"L\"\n"),
            catalogText: WithCompanion);

        Assert.That(result.Problems.Single(),
            Does.Contain("missing its companion file 'fiddle/npcs/author_mod_luna.toml'"));
        // the doc is the explanation the author reads
        Assert.That(result.Problems.Single(), Does.Contain("crashes during Setup"));
    }

    [Test]
    public void ShouldAcceptARegistrationWhoseCompanionIsPresent()
    {
        var result = Collect(new Dictionary<string, string>
        {
            ["momi/extensions/roster/luna.toml"] = "object = \"o\"\nlabel = \"L\"\n",
            ["fiddle/npcs/author_mod_luna.toml"] = "name = \"Luna\"\n",
            // the companion by existence, the object advisory by content
            ["gml/author_mod_luna_object.gml"] = "object_create(\"o\", undefined, {});\n",
        }, catalogText: WithCompanion);

        Assert.That(result.Problems, Is.Empty);
        Assert.That(result.Findings, Is.Empty);
        Assert.That(result.Registrations, Has.Count.EqualTo(1));
    }

    [Test]
    public void ShouldOnlyWarnWhenAWarningLevelCompanionIsMissing()
    {
        var result = Collect(new Dictionary<string, string>
        {
            ["momi/extensions/roster/luna.toml"] = "object = \"o\"\nlabel = \"L\"\n",
            ["fiddle/npcs/author_mod_luna.toml"] = "name = \"Luna\"\n",
        }, catalogText: WithCompanion);

        Assert.That(result.Problems, Is.Empty);
        Assert.That(result.Registrations, Has.Count.EqualTo(1), "a warning must not drop the mod");
        // two advisories, the missing warning-level companion, and the object
        // never appearing in an object_create call (no gml at all here)
        Assert.That(result.Findings, Has.Count.EqualTo(2));
        Assert.That(result.Findings.Select(f => f.Message).ToList(),
            Has.One.Contains("author_mod_luna_object.gml").And.One.Contains("names object 'o'"));
    }

    [Test]
    public void ShouldAcceptACompanionNamedWithTheLocalName()
    {
        // the install-time local-name pass renames fiddle/npcs/luna.toml to
        // the symbol form, so shipping the local spelling satisfies the
        // companion requirement
        var result = Collect(new Dictionary<string, string>
        {
            ["momi/extensions/roster/luna.toml"] = "object = \"o\"\nlabel = \"L\"\n",
            ["fiddle/npcs/luna.toml"] = "name = \"Luna\"\n",
            ["gml/author_mod_luna_object.gml"] = "object_create(\"o\", undefined, {});\n",
        }, catalogText: WithCompanion);

        Assert.That(result.Problems, Is.Empty);
        Assert.That(result.Findings, Is.Empty);
        Assert.That(result.Registrations, Has.Count.EqualTo(1));
    }

    [Test]
    public void ShouldCheckCompanionsPerRegistrantSymbol()
    {
        // one registrant satisfied, the other not. The check is per symbol,
        // and one failure still takes the whole mod down
        var result = Collect(new Dictionary<string, string>
        {
            ["momi/extensions/roster/luna.toml"] = "object = \"o\"\nlabel = \"L\"\n",
            ["momi/extensions/roster/wren.toml"] = "object = \"o\"\nlabel = \"W\"\n",
            ["fiddle/npcs/author_mod_luna.toml"] = "name = \"Luna\"\n",
        }, catalogText: WithCompanion);

        Assert.That(result.Problems.Where(p => p.Contains("fiddle/npcs")).ToList(), Has.Count.EqualTo(1));
        Assert.That(result.Problems.First(p => p.Contains("fiddle/npcs")),
            Does.Contain("author_mod_wren"));
    }

    [Test]
    public void ShouldReportEveryProblemRatherThanStoppingAtTheFirst()
    {
        var result = Collect(new Dictionary<string, string>
        {
            ["momi/extensions/roster/luna.toml"] = "object = \"bad token\"\nlabel = \"L\"\n",
            ["momi/extensions/roster/wren.toml"] = "object = \"o\"\n",
        });

        Assert.That(result.Problems, Has.Count.EqualTo(2));
        Assert.That(result.Registrations, Is.Empty);
    }

    [Test]
    public void ShouldWarnOnALetterSenderNothingProvides()
    {
        var mod = new MockMod(new Dictionary<string, object>
        {
            ["fiddle/letters.toml"] = "[hello]\nnpc = \"lunna\"\n",
        }) { Id = "author.mod" };

        List<LintFinding> findings = [];
        ExtensionCollector.CheckLetterSenders(mod,
            new HashSet<string>(StringComparer.Ordinal) { "adeline" }, findings);

        Assert.That(findings, Has.Count.EqualTo(1));
        Assert.That(findings[0].Message, Does.Contain("'lunna'"));
        Assert.That(findings[0].Line, Is.EqualTo(1));
    }

    [Test]
    public void ShouldAcceptVanillaAndRegisteredLetterSenders()
    {
        var mod = new MockMod(new Dictionary<string, object>
        {
            ["fiddle/letters.toml"] = "[a]\nnpc = \"adeline\"\n\n[b]\nnpc = \"author_mod_luna\"\n",
        }) { Id = "author.mod" };

        List<LintFinding> findings = [];
        ExtensionCollector.CheckLetterSenders(mod,
            new HashSet<string>(StringComparer.Ordinal) { "adeline", "author_mod_luna" }, findings);

        Assert.That(findings, Is.Empty);
    }

    [Test]
    public void ShouldReadVanillaSenderNamesFromThePristineEnum()
    {
        var catalogText = Catalog.Replace("id   = \"roster\"", "id   = \"npc_roster\"");
        var catalog = SeamCatalogLoader.Load(Encoding.UTF8.GetBytes(catalogText), "seams.toml");
        var pristine = new MemoryPristineSource(new Dictionary<string, byte[]>
        {
            ["assets/gml/NpcId.gml"] = "enum NpcId {\n    Adeline,\n    BigMoleDude,\n    LEN\n}\n"u8.ToArray(),
        });

        var names = ExtensionCollector.NpcNativeNames(catalog, pristine);

        Assert.That(names, Is.EquivalentTo(new[] { "adeline", "big_mole_dude" }));
    }
}
