using System.Text;
using Garethp.ModsOfMistriaInstallerLib.Seam;

namespace ModsOfMistriaInstallerLibTests.Seam;

public class MonsterVanillaRosterTest
{
    private static MemoryPristineSource Pristine() => new(new Dictionary<string, byte[]>
    {
        ["assets/fiddle/monsters/shroom.toml"] =
            Encoding.UTF8.GetBytes("""
                [default]
                gm_object = "obj_monster_shroom"
                [mushroom]
                gm_object = "obj_monster_mushroom_large"
                [mushroom.sprites]
                idle = "spr_mushroom"
                walk = "spr_mushroom"
                celebration = "spr_mushroom"
                [mushroom_green.sprites]
                idle = "spr_mushroom_green"
                walk = "spr_mushroom_green"
                """),
        ["assets/fiddle/monsters/bat.toml"] =
            Encoding.UTF8.GetBytes("[default]\n[bat]\n"),
        ["assets/fiddle/monsters/empty.toml"] =
            Encoding.UTF8.GetBytes("[default]\n"),
    });

    [Test]
    public void ShouldIndexPristineNamesByTheirSourceCategory()
    {
        var index = MonsterVanillaRoster.Index(Pristine());

        Assert.That(index.Monsters.Keys, Is.EqualTo(new[] { "bat", "mushroom", "mushroom_green" }));
        Assert.That(index.Monsters["mushroom"], Is.EqualTo("shroom"));
        Assert.That(index.Categories, Is.EquivalentTo(new[] { "bat", "empty", "shroom" }));
        Assert.That(index.Monsters, Does.Not.ContainKey("default"));
    }

    [Test]
    public void ShouldDescribeBuiltInCategoryStatesAndObjects()
    {
        var index = MonsterVanillaRoster.Index(Pristine());

        Assert.That(index.CategoryData["shroom"].RequiredStates, Is.EqualTo(new[] { "idle", "walk" }));
        Assert.That(index.CategoryData["shroom"].Objects,
            Is.EquivalentTo(new[] { "obj_monster_mushroom_large", "obj_monster_shroom" }));
    }

    [Test]
    public void ShouldRefuseAKeyThatAppearsInTwoPristineCategories()
    {
        var pristine = new MemoryPristineSource(new Dictionary<string, byte[]>
        {
            ["assets/fiddle/monsters/first.toml"] = Encoding.UTF8.GetBytes("[same]\n"),
            ["assets/fiddle/monsters/second.toml"] = Encoding.UTF8.GetBytes("[same]\n"),
        });

        Assert.That(() => MonsterVanillaRoster.Index(pristine),
            Throws.InvalidOperationException.With.Message.Contains("appears in categories 'first' and 'second'"));
    }

    [Test]
    public void ShouldRefuseAnEmptyPristineMonsterRoster()
    {
        var pristine = new MemoryPristineSource(new Dictionary<string, byte[]>());

        Assert.That(() => MonsterVanillaRoster.Index(pristine),
            Throws.InvalidOperationException.With.Message.Contains("no monster keys"));
    }
}
