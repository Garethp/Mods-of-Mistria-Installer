using Garethp.ModsOfMistriaInstallerLib.Collector;
using Garethp.ModsOfMistriaInstallerLib.Seam;
using ModsOfMistriaInstallerLibTests.Fixtures;

namespace ModsOfMistriaInstallerLibTests.Seam;

public class MonsterRegistryRendererTest
{
    [Test]
    public void ShouldRenderTheInstalledMonsterCatalogInNameOrder()
    {
        var first = new MockMod(new List<string>()) { Id = "z.mod" };
        var second = new MockMod(new List<string>()) { Id = "a.mod" };
        var collection = new MonsterCollection(
            [
                new MonsterDefinition(first, "zebra", "zoo", "obj_zebra", "z.toml", new HashSet<string>()),
                new MonsterDefinition(second, "ant", "anthill", "obj_ant", "a.toml", new HashSet<string>()),
            ],
            [
                new MonsterCategoryDefinition(first, "zoo", "zoo.toml", ["idle", "attack"]),
                new MonsterCategoryDefinition(second, "anthill", "anthill.toml", ["walk"]),
            ],
            [], [], ["bat", "mushroom"], []);

        var registry = MonsterRegistryRenderer.Render(collection);

        Assert.That(registry, Does.Contain("global.__mmapi_custom_monsters_enabled = true;"));
        Assert.That(registry, Does.Contain("__mmapi_vanilla_monster_keys[$ \"bat\"] = true;"));
        Assert.That(registry, Does.Contain("__mmapi_custom_monster_owners[$ \"ant\"] = \"a.mod\";"));
        Assert.That(registry.IndexOf("[$ \"ant\"]", StringComparison.Ordinal),
            Is.LessThan(registry.IndexOf("[$ \"zebra\"]", StringComparison.Ordinal)));
        Assert.That(registry, Does.Contain(
            "key: \"anthill\", mod_name: \"a.mod\", state_names: [\"walk\"]"));
        Assert.That(registry.IndexOf("key: \"anthill\"", StringComparison.Ordinal),
            Is.LessThan(registry.IndexOf("key: \"zoo\"", StringComparison.Ordinal)));
    }
}
