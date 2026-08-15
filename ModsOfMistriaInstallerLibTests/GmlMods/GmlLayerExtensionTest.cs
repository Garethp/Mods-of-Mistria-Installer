using System.Text;
using Garethp.ModsOfMistriaInstallerLib.GmlMods;
using Garethp.ModsOfMistriaInstallerLib.Seam;
using ModsOfMistriaInstallerLibTests.TestUtils;

namespace ModsOfMistriaInstallerLibTests.GmlMods;

// The exclusion fixpoint. A dropped mod's generated engine lines must go with
// it, because generated code referencing an absent mod's object is exactly the
// breakage this design exists to prevent.
[TestFixture]
public class GmlLayerExtensionTest
{
    // the synthetic catalog plus a point over an enum in the pristine fixture
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

        [[extension.vacancy_files]]
        path    = "data/{{symbol}}.toml"
        content = "vacant = true\n"
        """ + "\n";

    private const string OtherWithEnum =
        "enum Thing {\n    Alpha,\n    LEN\n}\n\n" + SyntheticLayer.PristineOther;

    private static SeamCatalog Catalog() =>
        SeamCatalogLoader.Load(Encoding.UTF8.GetBytes(CatalogWithPoint), "synthetic");

    private static MemoryPristineSource Pristine() => SyntheticLayer.Pristine(other: OtherWithEnum);

    private static ExtensionRegistration Reg(string modId, string symbol) =>
        new("roster", symbol, symbol, modId, new Dictionary<string, string> { ["object"] = "obj_x" });

    private static string EnumText(GmlLayerPlan plan) =>
        plan.Seamed["assets/gml/objects/Other.gml"].Text;

    [Test]
    public void ShouldSpliceASurvivingModsRegistration()
    {
        var plan = GmlLayer.Stage(Catalog(), Pristine(), [SyntheticLayer.Mod("testmod")], null,
            null, [Reg("testmod", "testmod_luna")], new MemoryExtensionLedger());

        Assert.That(EnumText(plan), Does.Contain("testmod_luna = 1,"));
        Assert.That(plan.Added, Contains.Key(ExtensionRegistryRenderer.RegistryRel));
        Assert.That(plan.NewAssignments.Single().Assignment.Symbol, Is.EqualTo("testmod_luna"));
    }

    [Test]
    public void ShouldDropAnExcludedModsGeneratedLines()
    {
        // The fixpoint case. The compile gate drops the mod, so its enum
        // member and its registry entry must vanish with it. A stale line
        // here would name an object that is no longer installed.
        var gate = new ScriptedGate
        {
            Fails = (mode, paths) =>
                mode == "unit" && paths.Any(p => p.Contains("badmod")) ? "boom" : null,
        };

        var plan = GmlLayer.Stage(Catalog(), Pristine(),
            [SyntheticLayer.Mod("goodmod"), SyntheticLayer.Mod("badmod")], gate,
            null,
            [Reg("goodmod", "goodmod_luna"), Reg("badmod", "badmod_wren")],
            new MemoryExtensionLedger());

        Assert.That(plan.Survivors.Select(m => m.Id), Is.EqualTo(new[] { "goodmod" }));
        Assert.That(EnumText(plan), Does.Contain("goodmod_luna"));
        Assert.That(EnumText(plan), Does.Not.Contain("badmod_wren"));

        var registry = Encoding.UTF8.GetString(plan.Added[ExtensionRegistryRenderer.RegistryRel]);
        Assert.That(registry, Does.Not.Contain("badmod_wren"));
    }

    [Test]
    public void ShouldNotBurnAnOrdinalOnAModItDropped()
    {
        // the ledger is written from NewAssignments after the loop settles, so
        // a dropped mod must not appear there, because an ordinal handed to a mod
        // that never installed would be a permanent tombstone for nothing
        var gate = new ScriptedGate
        {
            Fails = (mode, paths) =>
                mode == "unit" && paths.Any(p => p.Contains("badmod")) ? "boom" : null,
        };

        var plan = GmlLayer.Stage(Catalog(), Pristine(),
            [SyntheticLayer.Mod("badmod"), SyntheticLayer.Mod("goodmod")], gate,
            null,
            [Reg("goodmod", "goodmod_luna"), Reg("badmod", "badmod_wren")],
            new MemoryExtensionLedger());

        Assert.That(plan.NewAssignments.Select(a => a.Assignment.Symbol),
            Is.EqualTo(new[] { "goodmod_luna" }));
    }

    [Test]
    public void ShouldGiveTheSurvivorTheLowestOrdinalAfterADrop()
    {
        // re-derivation, not patching. With badmod gone, goodmod_luna is
        // assigned as though badmod had never been considered
        var gate = new ScriptedGate
        {
            Fails = (mode, paths) =>
                mode == "unit" && paths.Any(p => p.Contains("badmod")) ? "boom" : null,
        };

        var plan = GmlLayer.Stage(Catalog(), Pristine(),
            [SyntheticLayer.Mod("badmod"), SyntheticLayer.Mod("goodmod")], gate,
            null,
            [Reg("badmod", "aaa_wren"), Reg("goodmod", "zzz_luna")],
            new MemoryExtensionLedger());

        // aaa_wren would have sorted first and taken ordinal 1
        Assert.That(EnumText(plan), Does.Contain("zzz_luna = 1,"));
        Assert.That(EnumText(plan), Does.Not.Contain("aaa_wren"));
    }

    [Test]
    public void ShouldRenderAVacancyForALedgerSymbolWhoseModIsNotInstalled()
    {
        var ledger = new MemoryExtensionLedger(
            ("roster", new ExtensionAssignment("gone_luna", 1, "gone.mod")));

        var plan = GmlLayer.Stage(Catalog(), Pristine(), [SyntheticLayer.Mod("testmod")], null,
            null, [], ledger);

        Assert.That(EnumText(plan), Does.Contain("gone_luna = 1, // mmapi_ext:roster:enum_member:gone_luna:vacant"));
        Assert.That(plan.Added, Contains.Key("assets/data/gone_luna.toml"));
        // nothing live, but the registry still ships, because its vacant table is what
        // keeps the tombstone out of the journal
        Assert.That(plan.Added, Contains.Key(ExtensionRegistryRenderer.RegistryRel));
        Assert.That(Encoding.UTF8.GetString(plan.Added[ExtensionRegistryRenderer.RegistryRel]),
            Does.Contain("\"gone_luna\", 1,"));
    }

    [Test]
    public void ShouldStageAModWithRegistrationsAndNoGmlOfItsOwn()
    {
        var mod = SyntheticLayer.Mod("dataonly");
        var registrationOnly = new GmlModCode(mod.Mod, "dataonly", []);

        var plan = GmlLayer.Stage(Catalog(), Pristine(), [registrationOnly], new ScriptedGate(),
            null, [Reg("dataonly", "dataonly_luna")], new MemoryExtensionLedger());

        Assert.That(plan.Survivors.Select(m => m.Id), Is.EqualTo(new[] { "dataonly" }));
        Assert.That(EnumText(plan), Does.Contain("dataonly_luna = 1,"));
    }

    [Test]
    public void ShouldLeaveTheStageUntouchedWithNoRegistrationsAtAll()
    {
        // the same catalog, the same point, no registrants. The layer is what
        // it would be without the mechanism
        var withPoint = GmlLayer.Stage(Catalog(), Pristine(), [SyntheticLayer.Mod("testmod")], null);
        var withoutPoint = GmlLayer.Stage(SyntheticLayer.Catalog(), Pristine(),
            [SyntheticLayer.Mod("testmod")], null);

        Assert.That(EnumText(withPoint), Is.EqualTo(EnumText(withoutPoint)));
        Assert.That(withPoint.Added.Keys.Order(StringComparer.Ordinal),
            Is.EqualTo(withoutPoint.Added.Keys.Order(StringComparer.Ordinal)));
    }

    [Test]
    public void ShouldRunTheGateOnceWhenNothingIsDropped()
    {
        // the loop must not cost a second compile pass in the common case
        var gate = new ScriptedGate();

        GmlLayer.Stage(Catalog(), Pristine(), [SyntheticLayer.Mod("testmod")], gate,
            null, [Reg("testmod", "testmod_luna")], new MemoryExtensionLedger());

        Assert.That(gate.Calls.Count(c => c.Mode == "files"), Is.EqualTo(1));
    }

    [Test]
    public void ShouldNotReExcludeAModOnASecondRound()
    {
        // exclusions accumulate across rounds. A mod dropped in round 1 must
        // not be re-reported in round 2
        var gate = new ScriptedGate
        {
            Fails = (mode, paths) =>
                mode == "unit" && paths.Any(p => p.Contains("badmod")) ? "boom" : null,
        };

        var plan = GmlLayer.Stage(Catalog(), Pristine(),
            [SyntheticLayer.Mod("goodmod"), SyntheticLayer.Mod("badmod")], gate,
            null,
            [Reg("goodmod", "goodmod_luna"), Reg("badmod", "badmod_wren")],
            new MemoryExtensionLedger());

        Assert.That(plan.Excluded.Select(e => e.Mod.Id), Is.EqualTo(new[] { "badmod" }));
    }
}
