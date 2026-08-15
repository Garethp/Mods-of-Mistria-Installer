using System.Text;
using Garethp.ModsOfMistriaInstallerLib.Seam;

namespace ModsOfMistriaInstallerLibTests.Seam;

// Vacancy data emission and the generated registry.
[TestFixture]
public class ExtensionVacancyAndRegistryTest
{
    private const string NpcIdRel = "assets/gml/NpcId.gml";

    private const string NpcIdPristine =
        "enum NpcId {\n    Adeline,\n    Balor,\n    LEN\n}\n";

    private const string Point = """
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

        [[extension.sites]]
        id       = "enum_member"
        kind     = "enum_member"
        template = "{{symbol}} = {{ordinal}},"
        indent   = 4

        [extension.vacancy]
        enum_member = "{{symbol}} = {{ordinal}},"

        [[extension.vacancy_files]]
        path    = "fiddle/npcs/{{symbol}}.toml"
        content = '''
        name = "Departed Villager"
        ordinal = {{ordinal}}
        '''
        """ + "\n";

    private static SeamCatalog Load(string text) =>
        SeamCatalogLoader.Load(Encoding.UTF8.GetBytes(text), "seams.toml");

    private static MemoryPristineSource Pristine(params (string Rel, string Text)[] extra) =>
        new(extra.Append((Rel: NpcIdRel, Text: NpcIdPristine))
            .ToDictionary(f => f.Rel, f => Encoding.UTF8.GetBytes(f.Text)));

    private static ExtensionRegistration Reg(string symbol, string obj = "obj_x") =>
        new("roster", symbol, symbol, "mod.x", new Dictionary<string, string> { ["object"] = obj });

    private static Dictionary<string, byte[]> Run(
        IReadOnlyList<ExtensionRegistration> regs,
        IExtensionLedger? ledger = null,
        IPristineSource? pristine = null,
        string? catalogText = null)
    {
        var catalog = Load(catalogText ?? Point);
        var source = pristine ?? Pristine();
        var staged = SeamStager.Simulate(catalog, source);
        return ExtensionExpander.Expand(catalog, regs, ledger ?? new MemoryExtensionLedger(),
            staged, source).Added;
    }

    private static string Text(Dictionary<string, byte[]> added, string rel) =>
        Encoding.UTF8.GetString(added[rel]);

    [Test]
    public void ShouldEmitAStubForAVacancyWithPlaceholdersSubstituted()
    {
        var ledger = new MemoryExtensionLedger(("roster", new ExtensionAssignment("modx_luna", 2, "mod.x")));

        var added = Run([], ledger);

        Assert.That(added.Keys, Does.Contain("assets/fiddle/npcs/modx_luna.toml"));
        var stub = Text(added, "assets/fiddle/npcs/modx_luna.toml");
        Assert.That(stub, Does.Contain("name = \"Departed Villager\""));
        Assert.That(stub, Does.Contain("ordinal = 2"));
    }

    [Test]
    public void ShouldEmitNoStubForALiveRegistration()
    {
        // the mod ships its own real data, and a stub would overwrite it
        var ledger = new MemoryExtensionLedger(("roster", new ExtensionAssignment("modx_luna", 2, "mod.x")));

        var added = Run([Reg("modx_luna")], ledger);

        Assert.That(added.Keys, Has.No.Member("assets/fiddle/npcs/modx_luna.toml"));
    }

    [Test]
    public void ShouldEmitNothingAtAllWithNoRegistrantsAndNoVacancies()
    {
        // the inertness invariant, over the added set as well as the seamed one
        Assert.That(Run([]), Is.Empty);
    }

    [Test]
    public void ShouldRefuseToOverwriteAnExistingArchiveEntry()
    {
        var ledger = new MemoryExtensionLedger(("roster", new ExtensionAssignment("modx_luna", 2, "mod.x")));
        var pristine = Pristine(("assets/fiddle/npcs/modx_luna.toml", "name = \"Real\"\n"));

        var exception = Assert.Throws<SeamStagingException>(() => Run([], ledger, pristine));

        Assert.That(exception!.Message, Does.Contain("would overwrite the existing archive entry"));
        Assert.That(exception.Problems.Single().Kind, Is.EqualTo(SeamProblemKind.Extension));
    }

    [Test]
    public void ShouldEmitTheRegistryOnlyWhenSomethingIsRegistered()
    {
        Assert.That(Run([]).Keys, Has.No.Member(ExtensionRegistryRenderer.RegistryRel));
        Assert.That(Run([Reg("modx_luna")]).Keys, Does.Contain(ExtensionRegistryRenderer.RegistryRel));
    }

    [Test]
    public void ShouldListLiveRegistrantsInTheRegistry()
    {
        var added = Run([Reg("moda_luna"), Reg("modz_wren")]);

        var gml = Text(added, ExtensionRegistryRenderer.RegistryRel);
        Assert.That(gml, Does.Contain("\"roster\", \"moda_luna\", 2,"));
        Assert.That(gml, Does.Contain("\"roster\", \"modz_wren\", 3,"));
    }

    [Test]
    public void ShouldSplitLiveAndVacantBetweenTheTwoCatalogs()
    {
        // live symbols answer "is this here" through mmapi_ext_catalog. A
        // vacancy is not there (the mod is gone, and the registry saying yes
        // would be a lie) but is in the vacant catalog, which is what the
        // npc_is_unlocked seam consults to keep tombstones out of the journal
        var ledger = new MemoryExtensionLedger(
            ("roster", new ExtensionAssignment("modx_gone", 2, "mod.gone")),
            ("roster", new ExtensionAssignment("modx_here", 3, "mod.here")));

        var added = Run([Reg("modx_here")], ledger);

        var gml = Text(added, ExtensionRegistryRenderer.RegistryRel);
        var live = gml[..gml.IndexOf("mmapi_ext_vacant_catalog", StringComparison.Ordinal)];
        var vacant = gml[gml.IndexOf("mmapi_ext_vacant_catalog", StringComparison.Ordinal)..];
        Assert.That(live, Does.Contain("\"modx_here\", 3,"));
        Assert.That(live, Does.Not.Contain("modx_gone"));
        Assert.That(vacant, Does.Contain("\"modx_gone\", 2,"));
        Assert.That(vacant, Does.Not.Contain("modx_here"));
        // ...and the vacancy's stub data is emitted, because its member exists
        Assert.That(added.Keys, Does.Contain("assets/fiddle/npcs/modx_gone.toml"));
    }

    [Test]
    public void ShouldRenderARegistryThatParsesAsGml()
    {
        var added = Run([Reg("modx_luna")]);

        var gml = Text(added, ExtensionRegistryRenderer.RegistryRel);
        var exports = GmlScanner.TopLevelDefinitions(gml)
            .Where(s => s.Form == FunctionForm.Decl)
            .Select(s => s.Name)
            .ToList();
        Assert.That(exports, Is.EquivalentTo(new[]
        {
            "mmapi_ext_catalog", "mmapi_ext_id", "mmapi_ext_symbol", "mmapi_ext_ids",
            "mmapi_ext_vacant_catalog",
        }));
    }

    [Test]
    public void ShouldCallOnlyAttestedBuiltinsFromTheRegistry()
    {
        // Every call in the rendered file must be self-defined or an
        // engine-attested builtin.
        var gml = Text(Run([Reg("modx_luna")]), ExtensionRegistryRenderer.RegistryRel);

        var defined = GmlScanner.TopLevelDefinitions(gml).Select(s => s.Name).ToHashSet();
        var attested = new HashSet<string> { "array_length", "array_push", "string" };
        // control-flow keywords tokenize like call sites (`for (`, `if (`)
        var keywords = new HashSet<string> { "if", "for", "while", "switch", "repeat", "with", "catch", "until" };
        var unknown = GmlScanner.FindPrefixedCalls(gml, [""])
            .Select(c => c.Name)
            .Where(n => !defined.Contains(n) && !attested.Contains(n) && !keywords.Contains(n))
            .Distinct()
            .ToList();

        Assert.That(unknown, Is.Empty,
            "the registry calls names neither self-defined nor attested in the engine dialect");
    }

    [Test]
    public void ShouldRenderTheRegistryDeterministically()
    {
        var forwards = Text(Run([Reg("moda_luna"), Reg("modz_wren")]),
            ExtensionRegistryRenderer.RegistryRel);
        var backwards = Text(Run([Reg("modz_wren"), Reg("moda_luna")]),
            ExtensionRegistryRenderer.RegistryRel);

        Assert.That(backwards, Is.EqualTo(forwards));
    }
}
