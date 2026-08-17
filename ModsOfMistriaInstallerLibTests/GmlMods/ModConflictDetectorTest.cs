using Garethp.ModsOfMistriaInstallerLib.GmlMods;
using Garethp.ModsOfMistriaInstallerLib.Seam;
using ModsOfMistriaInstallerLibTests.Fixtures;

namespace ModsOfMistriaInstallerLibTests.GmlMods;

[TestFixture]
public class ModConflictDetectorTest
{
    [Test]
    public void FindsExclusiveHookConflictWithoutAnArchive()
    {
        var catalog = new SeamCatalog(1, [],
        [
            new HookDeclaration("calc.max", HookKind.Override, "test", HookProvider.Seam, [], false,
                HookContention.Exclusive)
        ], []);
        var alpha = new MockMod(new Dictionary<string, object>
        {
            ["gml/Main.gml"] = "mmapi_override(\"calc.max\", alpha_handler);"
        }) { Id = "alpha" };
        var beta = new MockMod(new Dictionary<string, object>
        {
            ["gml/Main.gml"] = "mmapi_override(\"calc.max\", beta_handler);"
        }) { Id = "beta" };

        var conflicts = ModConflictDetector.Find([alpha, beta], catalog);

        Assert.That(conflicts, Has.Count.EqualTo(1));
        Assert.That(conflicts[0].Key, Is.EqualTo("calc.max"));
        Assert.That(conflicts[0].ModIds, Is.EquivalentTo(new[] { "alpha", "beta" }));
    }
}
