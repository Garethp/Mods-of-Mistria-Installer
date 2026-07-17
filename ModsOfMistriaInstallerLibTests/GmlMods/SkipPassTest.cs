using Garethp.ModsOfMistriaInstallerLib.GmlMods;
using ModsOfMistriaInstallerLibTests.Fixtures;
using ModsOfMistriaInstallerLibTests.TestUtils;

namespace ModsOfMistriaInstallerLibTests.GmlMods;

// The symbol skip pass, driven through the layer's staging entry: a mod that
// would break the engine compile is excluded with a report so the game still
// boots with everything else.
[TestFixture]
public class SkipPassTest
{
    private static GmlLayerPlan Stage(GmlLayerOptions? options = null, params GmlModCode[] mods) =>
        GmlLayer.Stage(SyntheticLayer.Catalog(), SyntheticLayer.Pristine(), mods, null, options);

    [Test]
    public void ShouldSkipAModCollidingWithTheEngine()
    {
        // `helper` is a pristine engine export (Other.gml). A mod redefining
        // it would be a duplicate-export compile error at boot.
        var rogue = SyntheticLayer.Mod("rogue", "function helper() {\n}\n");
        var plan = Stage(null, SyntheticLayer.Mod("testmod"), rogue);

        Assert.That(plan.Excluded.Select(e => e.Mod.Id), Is.EqualTo(new[] { "rogue" }));
        Assert.That(plan.Excluded[0].Reasons[0], Does.Contain("helper"));
        Assert.That(plan.Excluded[0].Reasons[0], Does.Contain("the engine"));
        Assert.That(plan.Added.Keys, Has.None.StartWith("assets/gml/scripts/rogue/"));
        Assert.That(plan.Added, Contains.Key("assets/gml/scripts/testmod/core/State.gml"));
    }

    [Test]
    public void ShouldSkipTheLaterOfTwoCollidingMods()
    {
        var first = SyntheticLayer.Mod("first", "function shared_fn() {\n}\n");
        var second = SyntheticLayer.Mod("second", "function shared_fn() {\n}\n");

        var plan = Stage(null, first, second);

        Assert.That(plan.Excluded.Select(e => e.Mod.Id), Is.EqualTo(new[] { "second" }));
        Assert.That(plan.Excluded[0].Reasons[0], Does.Contain("mod 'first'"));
        Assert.That(plan.Added, Contains.Key("assets/gml/scripts/first/core/State.gml"));
        Assert.That(plan.Added.Keys, Has.None.StartWith("assets/gml/scripts/second/"));
    }

    [Test]
    public void ShouldSkipAModCollidingWithTheFramework()
    {
        // mmapi_emit is a carried framework export
        var rogue = SyntheticLayer.Mod("rogue", "function mmapi_emit(hook, ctx) {\n}\n");

        var plan = Stage(null, rogue);

        Assert.That(plan.Excluded, Has.Count.EqualTo(1));
        Assert.That(plan.Excluded[0].Reasons[0], Does.Contain("the mmapi framework"));
    }

    [Test]
    public void ShouldNotTreatAnMmapiPrefixedSymbolAsTheFramework()
    {
        // regression: the framework-prefix match must not swallow
        // scripts/mmapi_tools/, or the mod's own functions read as collisions
        // with itself
        var mod = SyntheticLayer.Mod("mmapi_tools", "function mmapi_tools_boot() {\n}\n");

        var plan = Stage(null, mod);

        Assert.That(plan.Excluded, Is.Empty);
        Assert.That(plan.Added, Contains.Key("assets/gml/scripts/mmapi_tools/core/State.gml"));
    }

    [Test]
    public void ShouldSkipAnIntraModDuplicateExport()
    {
        var mock = new MockMod(new Dictionary<string, string>
        {
            { "gml/core/State.gml", "function dupmod_util() {\n}\n" },
            { "gml/core/Second.gml", "function dupmod_util() {\n}\n" },
        }) { Id = "dupmod", DirName = "dupmod" };
        var dup = GmlModCollector.Collect(mock)!;

        var plan = Stage(null, dup);

        Assert.That(plan.Excluded.Select(e => e.Mod.Id), Is.EqualTo(new[] { "dupmod" }));
        Assert.That(plan.Excluded[0].Reasons[0], Does.Contain("more than once"));
        Assert.That(plan.Added.Keys, Has.None.StartWith("assets/gml/scripts/dupmod/"));
    }

    [Test]
    public void ShouldAbortOnFailOnSkip()
    {
        var rogue = SyntheticLayer.Mod("rogue", "function helper() {\n}\n");

        var exception = Assert.Throws<InvalidOperationException>(() =>
            Stage(new GmlLayerOptions { FailOnSkip = true }, rogue));

        Assert.That(exception!.Message, Does.Contain("fail-on-skip"));
        Assert.That(exception.Message, Does.Contain("helper"));
    }

    [Test]
    public void ShouldExcludeTheLaterModOnASymbolClash()
    {
        var first = SyntheticLayer.Mod("samemod", dirName: "ModA");
        var second = SyntheticLayer.Mod("samemod", dirName: "ModB");

        var plan = Stage(null, first, second);

        Assert.That(plan.Excluded, Has.Count.EqualTo(1));
        Assert.That(plan.Excluded[0].Reasons[0], Does.Contain("scripts/samemod/"));
        Assert.That(plan.Added, Contains.Key("assets/gml/scripts/samemod/core/State.gml"));
        Assert.That(plan.Survivors.Select(m => m.DirName), Is.EqualTo(new[] { "ModA" }));
    }

    [Test]
    public void ShouldExcludeAModClaimingTheMmapiNamespace()
    {
        var impostor = SyntheticLayer.Mod("mmapi", dirName: "Impostor");

        var plan = Stage(null, impostor);

        Assert.That(plan.Excluded, Has.Count.EqualTo(1));
        Assert.That(plan.Excluded[0].Reasons[0], Does.Contain("mmapi framework"));
        Assert.That(plan.Added, Contains.Key("assets/gml/scripts/mmapi/mmapi.gml"));
    }
}
