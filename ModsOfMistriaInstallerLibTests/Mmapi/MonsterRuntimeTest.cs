using System.Text;
using Garethp.ModsOfMistriaInstallerLib.Collector;
using Garethp.ModsOfMistriaInstallerLib.Seam;
using ModsOfMistriaInstallerLibTests.Fixtures;
using ModsOfMistriaInstallerLibTests.TestUtils;

namespace ModsOfMistriaInstallerLibTests.Mmapi;

[TestFixture]
public class MonsterRuntimeTest
{
    private static void AssertBodyPasses(MonsterCollection monsters, string body)
    {
        var source = new StringBuilder();
        foreach (var (_, bytes) in PayloadResolver.MmapiSources())
            source.AppendLine(Encoding.UTF8.GetString(bytes));
        if (monsters.Definitions.Count > 0)
            source.AppendLine(MonsterRegistryRenderer.Render(monsters));
        source.AppendLine(File.ReadAllText(
            GmlProbe.Fixture(Path.Combine("monster_runtime", "_monster_helpers.gml"))));
        source.AppendLine(File.ReadAllText(GmlProbe.Fixture("gml_prelude.gml")));
        source.AppendLine(File.ReadAllText(
            GmlProbe.Fixture(Path.Combine("monster_runtime", body))));

        var script = Path.Combine(Path.GetTempPath(), $"momi_monster_probe_{Guid.NewGuid():N}.gml");
        try
        {
            File.WriteAllText(script, source.ToString(), new UTF8Encoding(false));
            GmlProbe.AssertAllPass(GmlProbe.RunGml(script), body);
        }
        finally
        {
            if (File.Exists(script)) File.Delete(script);
        }
    }

    [Test]
    public void ShouldKeepCustomIdsOutOfVanillaKillPersistence()
    {
        var mod = new MockMod(new List<string>()) { Id = "example.enemy_roller" };
        var monsters = new MonsterCollection(
            [new MonsterDefinition(mod, "example_enemy_roller", "example_enemy_roller",
                "obj_example_enemy_roller", "roller.toml", new HashSet<string>())],
            [new MonsterCategoryDefinition(mod, "example_enemy_roller", "category.toml", ["idle"])],
            [], [], ["bat", "mushroom"], []);

        AssertBodyPasses(monsters, "custom_registry.gml");
    }

    [Test]
    public void ShouldLoadAvailableKillCountsWhenSavedMonsterNamesAreMissing()
    {
        AssertBodyPasses(MonsterCollection.Empty, "empty_registry.gml");
    }
}
