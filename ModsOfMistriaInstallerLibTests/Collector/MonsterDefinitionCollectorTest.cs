using System.Text;
using Garethp.ModsOfMistriaInstallerLib.Collector;
using Garethp.ModsOfMistriaInstallerLib.ModTypes;
using Garethp.ModsOfMistriaInstallerLib.Seam;
using ModsOfMistriaInstallerLibTests.Fixtures;

namespace ModsOfMistriaInstallerLibTests.Collector;

public class MonsterDefinitionCollectorTest
{
    private static readonly IReadOnlySet<string> NativeSprites =
        new HashSet<string>(["spr_native"], StringComparer.Ordinal);

    private static MemoryPristineSource Pristine(
        IReadOnlySet<string>? sprites = null,
        IReadOnlySet<string>? objects = null,
        bool includeRoster = true)
    {
        Dictionary<string, byte[]> files = [];
        if (includeRoster)
        {
            files["assets/fiddle/monsters/shroom.toml"] =
                Encoding.UTF8.GetBytes("""
                    [default]
                    hp = 10
                    damage = 2
                    essence = 0
                    iframes = 7
                    damage_number_offset = -15
                    patience_acknowledgement_reset = -120
                    aggro_radius = 192
                    status_effect_offset = -8
                    fire_effect_offset = -4
                    starting_dir = [0, 360]
                    gm_object = "obj_monster_mushroom"
                    hurtbox = "spr_native"
                    drops = []
                    [default.tango]
                    [mushroom]
                    [mushroom.sprites]
                    idle = "spr_native"
                    """);
            files["assets/fiddle/monsters/bat.toml"] =
                Encoding.UTF8.GetBytes("[default]\n[bat]\n");
        }
        HashSet<string> assets = new(sprites ?? NativeSprites, StringComparer.Ordinal);
        assets.UnionWith(objects ?? new HashSet<string>(["obj_monster_mushroom"], StringComparer.Ordinal));
        return new MemoryPristineSource(files, assets);
    }

    private static string Entry(string key = "roller", string objectName = "obj_roller",
        string sprite = "spr_native") => $"""
        [{key}]
        hp = 10
        damage = 2
        essence = 0
        iframes = 7
        damage_number_offset = -15
        patience_acknowledgement_reset = -120
        aggro_radius = 192
        status_effect_offset = -8
        fire_effect_offset = -4
        starting_dir = [0, 360]
        gm_object = "{objectName}"
        hurtbox = "{sprite}"
        drops = []
        [{key}.sprites]
        idle = "{sprite}"
        [{key}.tango]
        """;

    private static string ObjectGml(string objectName = "obj_roller", string parent = "par_monster") =>
        $"object_create(\"{objectName}\", object_reserve(\"{parent}\"), {{}});";

    private static MockMod Custom(string key = "roller", string category = "roller",
        string objectName = "obj_roller", string id = "mock.mod", string sprite = "spr_native") =>
        new(new Dictionary<string, object>
        {
            [$"momi/monster_categories/{category}.toml"] = "states = [\"idle\"]\n",
            [$"fiddle/monsters/{category}.toml"] = Entry(key, objectName, sprite),
            ["gml/Monster.gml"] = ObjectGml(objectName),
        }) { Id = id };

    private static bool HasProblem(MonsterCollection result, IMod mod, string text) =>
        result.Problems.Any(problem => problem.Mod == mod && problem.Message.Contains(text));

    [Test]
    public void ShouldCollectACustomMonsterAndVanillaKeys()
    {
        var mod = Custom();
        var result = MonsterDefinitionCollector.Collect([mod], Pristine());

        Assert.That(result.Problems, Is.Empty);
        Assert.That(result.Findings, Is.Empty);
        Assert.That(result.Definitions.Single().Key, Is.EqualTo("roller"));
        Assert.That(result.Categories.Single().States, Is.EqualTo(new[] { "idle" }));
        Assert.That(result.PristineKeys, Is.EqualTo(new[] { "bat", "mushroom" }));
    }

    [Test]
    public void ShouldPreserveObjectAndStateCasing()
    {
        var mod = Custom(objectName: "obj_ExampleRoller");
        mod.SetFile("momi/monster_categories/roller.toml", "states = [\"IdleState\"]\n");
        mod.SetFile("fiddle/monsters/roller.toml",
            Entry(objectName: "obj_ExampleRoller").Replace("idle =", "IdleState ="));

        var result = MonsterDefinitionCollector.Collect([mod], Pristine());

        Assert.That(result.Problems, Is.Empty);
        Assert.That(result.Categories.Single().States, Is.EqualTo(new[] { "IdleState" }));
    }

    [Test]
    public void ShouldLeaveVanillaPatchesOnTheExistingFiddlePath()
    {
        var first = new MockMod(new Dictionary<string, object>
        {
            ["fiddle/monsters/shroom.toml"] = "[mushroom]\nhp = 40\n",
        }) { Id = "first.mod" };
        var second = new MockMod(new Dictionary<string, object>
        {
            ["fiddle/monsters/shroom.toml"] = "[mushroom]\ndamage = 3\n",
        }) { Id = "second.mod" };

        var result = MonsterDefinitionCollector.Collect([first, second], Pristine());

        Assert.That(result.Problems, Is.Empty);
        Assert.That(result.Definitions, Is.Empty);
        Assert.That(result.Categories, Is.Empty);
        Assert.That(result.VanillaPatchCount, Is.EqualTo(2));
        Assert.That(result.ForMods(new HashSet<IMod> { first }).VanillaPatchCount, Is.EqualTo(1));
    }

    [Test]
    public void ShouldLeaveAVanillaSpriteReplacementOnTheExistingImagePath()
    {
        var mod = new MockMod(new Dictionary<string, object>
        {
            ["fiddle/monsters/shroom.toml"] =
                "[mushroom.sprites]\nidle = \"spr_monster_mushroom_idle\"\n",
            ["images/replace/spr_monster_mushroom_idle.png"] = Array.Empty<byte>(),
        });

        var result = MonsterDefinitionCollector.Collect([mod], Pristine());

        Assert.That(result.Problems, Is.Empty);
        Assert.That(result.Definitions, Is.Empty);
        Assert.That(result.VanillaPatchCount, Is.EqualTo(1));
    }

    [Test]
    public void ShouldRejectAVanillaKeyContributedUnderTheWrongCategory()
    {
        var mod = new MockMod(new Dictionary<string, object>
        {
            ["fiddle/monsters/bat.toml"] = "[mushroom]\nhp = 40\n",
        });

        var result = MonsterDefinitionCollector.Collect([mod], Pristine());

        Assert.That(HasProblem(result, mod, "belongs to built-in category 'shroom'"), Is.True);
        Assert.That(HasProblem(result, mod, "not 'bat'"), Is.True);
    }

    [Test]
    public void ShouldAcceptANewMonsterInAVanillaCategoryUsingItsDefaultObject()
    {
        var mod = new MockMod(new Dictionary<string, object>
        {
            ["fiddle/monsters/shroom.toml"] = """
                [glow_shroom]
                [glow_shroom.sprites]
                idle = "spr_native"
                """,
        });

        var result = MonsterDefinitionCollector.Collect([mod], Pristine());

        Assert.That(result.Problems, Is.Empty);
        Assert.That(result.Categories, Is.Empty);
        var definition = result.Definitions.Single();
        Assert.That(definition.Key, Is.EqualTo("glow_shroom"));
        Assert.That(definition.ObjectName, Is.EqualTo("obj_monster_mushroom"));
        Assert.That(definition.DefinesObject, Is.False);
    }

    [Test]
    public void ShouldAcceptANewObjectForAMonsterInAVanillaCategory()
    {
        var mod = new MockMod(new Dictionary<string, object>
        {
            ["fiddle/monsters/shroom.toml"] = Entry("glow_shroom", "obj_glow_shroom"),
            ["gml/Monster.gml"] = ObjectGml("obj_glow_shroom"),
        });

        var result = MonsterDefinitionCollector.Collect([mod], Pristine());

        Assert.That(result.Problems, Is.Empty);
        Assert.That(result.Categories, Is.Empty);
        Assert.That(result.Definitions.Single().DefinesObject, Is.True);
    }

    [Test]
    public void ShouldRequireTheBuiltInCategorySpriteStates()
    {
        var mod = new MockMod(new Dictionary<string, object>
        {
            ["fiddle/monsters/shroom.toml"] = """
                [glow_shroom]
                [glow_shroom.sprites]
                walk = "spr_native"
                """,
        });

        var result = MonsterDefinitionCollector.Collect([mod], Pristine());

        Assert.That(HasProblem(result, mod, "sprites.idle is required by the category"), Is.True);
    }

    [Test]
    public void ShouldNotBorrowAnObjectFromAnotherBuiltInCategory()
    {
        var mod = new MockMod(new Dictionary<string, object>
        {
            ["fiddle/monsters/shroom.toml"] = Entry("glow_shroom", "obj_monster_bat"),
        });
        var objects = new HashSet<string>(["obj_monster_mushroom", "obj_monster_bat"],
            StringComparer.Ordinal);

        var result = MonsterDefinitionCollector.Collect([mod], Pristine(objects: objects));

        Assert.That(HasProblem(result, mod, "uses built-in object 'obj_monster_bat'"), Is.True);
        Assert.That(HasProblem(result, mod, "use the category's object"), Is.True);
    }

    [Test]
    public void ShouldNotBorrowADefaultObjectFromAnotherMod()
    {
        var monster = new MockMod(new Dictionary<string, object>
        {
            ["fiddle/monsters/shroom.toml"] = """
                [glow_shroom]
                [glow_shroom.sprites]
                idle = "spr_native"
                """,
        }) { Id = "monster.mod" };
        var defaults = new MockMod(new Dictionary<string, object>
        {
            ["fiddle/monsters/shroom.toml"] = "[default]\ngm_object = \"obj_shared_shroom\"\n",
            ["gml/Monster.gml"] = ObjectGml("obj_shared_shroom"),
        }) { Id = "defaults.mod" };

        var result = MonsterDefinitionCollector.Collect([monster, defaults], Pristine());

        Assert.That(HasProblem(result, monster, "object 'obj_shared_shroom'"), Is.True);
        Assert.That(HasProblem(result, monster, "this mod has no gml/ source"), Is.True);
        Assert.That(result.Problems.Any(problem => problem.Mod == defaults), Is.False);
    }

    [Test]
    public void ShouldKeepEarlierNestedBuiltInDefaultsWhenApplyingLaterPatches()
    {
        var defaults = new MockMod(new Dictionary<string, object>
        {
            ["fiddle/monsters/shroom.toml"] = "[default.sprites]\nidle = \"spr_native\"\n",
        }) { Id = "defaults.mod" };
        var monster = new MockMod(new Dictionary<string, object>
        {
            ["fiddle/monsters/shroom.toml"] = """
                [default.sprites]
                walk = "spr_native"
                [glow_shroom]
                """,
        }) { Id = "monster.mod" };

        var result = MonsterDefinitionCollector.Collect([defaults, monster], Pristine());

        Assert.That(result.Problems, Is.Empty);
        Assert.That(result.Definitions.Single().ObjectName, Is.EqualTo("obj_monster_mushroom"));
    }

    [Test]
    public void ShouldRequireACategoryDeclarationFromTheMonsterMod()
    {
        var mod = new MockMod(new Dictionary<string, object>
        {
            ["fiddle/monsters/roller.toml"] = Entry(),
            ["gml/Monster.gml"] = ObjectGml(),
        });

        var result = MonsterDefinitionCollector.Collect([mod], Pristine());

        Assert.That(HasProblem(result, mod, "has no matching momi/monster_categories/roller.toml"), Is.True);
    }

    [Test]
    public void ShouldRejectDuplicateCustomKeysForEveryOwner()
    {
        var first = Custom("shared", "first", "obj_first", "first.mod");
        var second = Custom("shared", "second", "obj_second", "second.mod");

        var result = MonsterDefinitionCollector.Collect([first, second], Pristine());

        Assert.That(result.Definitions, Is.Empty);
        Assert.That(HasProblem(result, first, "monster key 'shared' is also declared by second.mod"), Is.True);
        Assert.That(HasProblem(result, second, "monster key 'shared' is also declared by first.mod"), Is.True);
    }

    [Test]
    public void ShouldNotLetAMalformedDefinitionHideADuplicateCustomKey()
    {
        var complete = Custom("shared", "first", "obj_first", "first.mod");
        var malformed = Custom("other", "second", "obj_second", "second.mod");
        malformed.SetFile("fiddle/monsters/second.toml", "shared = 4\n");

        var result = MonsterDefinitionCollector.Collect([complete, malformed], Pristine());

        Assert.That(HasProblem(result, complete, "monster key 'shared' is also declared by second.mod"), Is.True);
        Assert.That(HasProblem(result, malformed, "monster key 'shared' is also declared by first.mod"), Is.True);
        Assert.That(HasProblem(result, malformed, "[shared] must be a table"), Is.True);
    }

    [Test]
    public void ShouldAcceptTwoIndependentCustomMonstersInOneSelection()
    {
        var first = Custom("roller", "roller", "obj_roller", "first.mod");
        var second = Custom("wisp", "wisp", "obj_wisp", "second.mod");

        var result = MonsterDefinitionCollector.Collect([first, second], Pristine());

        Assert.That(result.Problems, Is.Empty);
        Assert.That(result.Definitions.Select(definition => definition.Key),
            Is.EqualTo(new[] { "roller", "wisp" }));
        Assert.That(result.Categories.Select(category => category.Key),
            Is.EqualTo(new[] { "roller", "wisp" }));
    }

    [Test]
    public void ShouldRejectSameModCustomKeyOccurrencesInDifferentCategories()
    {
        var mod = Custom("shared", "first", "obj_first");
        mod.SetFile("momi/monster_categories/second.toml", "states = [\"idle\"]\n");
        mod.SetFile("fiddle/monsters/second.toml", Entry("shared", "obj_second"));
        mod.SetFile("gml/Second.gml", ObjectGml("obj_second"));

        var result = MonsterDefinitionCollector.Collect([mod], Pristine());

        Assert.That(result.Problems.Count(problem => problem.Message.Contains("monster key 'shared' is also declared")),
            Is.EqualTo(2));
    }

    [Test]
    public void ShouldRejectDuplicateCategoriesForEveryDeclarer()
    {
        var first = Custom("first_monster", "shared", "obj_first", "first.mod");
        var second = Custom("second_monster", "shared", "obj_second", "second.mod");

        var result = MonsterDefinitionCollector.Collect([first, second], Pristine());

        Assert.That(HasProblem(result, first, "category 'shared' is also declared by second.mod"), Is.True);
        Assert.That(HasProblem(result, second, "category 'shared' is also declared by first.mod"), Is.True);
    }

    [Test]
    public void ShouldRejectOnlyTheForeignContributorToAPrivateCategory()
    {
        var owner = Custom("roller", "shared", "obj_roller", "owner.mod");
        var contributor = new MockMod(new Dictionary<string, object>
        {
            ["fiddle/monsters/shared.toml"] = "[default]\nhp = 99\n",
        }) { Id = "foreign.mod" };

        var result = MonsterDefinitionCollector.Collect([owner, contributor], Pristine());

        Assert.That(HasProblem(result, contributor, "category 'shared' belongs to mod 'owner.mod'"), Is.True);
        Assert.That(result.Problems.Any(problem => problem.Mod == owner), Is.False);
        Assert.That(result.Definitions.Single().Mod, Is.SameAs(owner));
    }

    [Test]
    public void ShouldRejectDuplicateCustomMonsterObjectsForEveryOwner()
    {
        var first = Custom("first", "first", "obj_shared", "first.mod");
        var second = Custom("second", "second", "obj_shared", "second.mod");

        var result = MonsterDefinitionCollector.Collect([first, second], Pristine());

        Assert.That(HasProblem(result, first, "monster object 'obj_shared' is also declared by second.mod"), Is.True);
        Assert.That(HasProblem(result, second, "monster object 'obj_shared' is also declared by first.mod"), Is.True);
    }

    [Test]
    public void ShouldAllowMonstersFromOneModToShareAnObject()
    {
        var mod = Custom(key: "roller_blue");
        mod.SetFile("fiddle/monsters/roller.toml",
            Entry("roller_blue") + "\n" + Entry("roller_red"));

        var result = MonsterDefinitionCollector.Collect([mod], Pristine());

        Assert.That(result.Problems, Is.Empty);
        Assert.That(result.Definitions.Select(definition => definition.Key),
            Is.EqualTo(new[] { "roller_blue", "roller_red" }));
        Assert.That(result.Definitions.Select(definition => definition.ObjectName).Distinct(),
            Is.EqualTo(new[] { "obj_roller" }));
    }

    [Test]
    public void ShouldRejectABuiltInObjectForACustomCategory()
    {
        var mod = Custom(objectName: "obj_monster_mushroom");

        var result = MonsterDefinitionCollector.Collect([mod], Pristine());

        Assert.That(HasProblem(result, mod, "uses built-in object 'obj_monster_mushroom'"), Is.True);
    }

    [Test]
    public void ShouldNotRejectForeignObjectClaimsDuringCollection()
    {
        var owner = Custom();
        var foreign = new MockMod(new Dictionary<string, object>
        {
            ["gml/Foreign.gml"] = ObjectGml(),
        }) { Id = "foreign.mod" };

        var result = MonsterDefinitionCollector.Collect([owner, foreign], Pristine());

        Assert.That(result.Problems, Is.Empty);
        Assert.That(result.Definitions.Single().Mod, Is.SameAs(owner));
    }

    [Test]
    public void ShouldRejectAConcreteWrongParent()
    {
        var wrong = Custom(id: "wrong.mod");
        wrong.SetFile("gml/Monster.gml", ObjectGml(parent: "par_npc"));

        var result = MonsterDefinitionCollector.Collect([wrong], Pristine());

        Assert.That(HasProblem(result, wrong, "inherits 'par_npc'"), Is.True);
    }

    [Test]
    public void ShouldReportIndirectObjectConstructionAsAWarning()
    {
        var indirect = Custom(key: "indirect", category: "indirect", objectName: "obj_indirect",
            id: "indirect.mod");
        indirect.SetFile("gml/Monster.gml", "function make_monster(_name) { return _name; }");

        var result = MonsterDefinitionCollector.Collect([indirect], Pristine());

        Assert.That(result.Problems, Is.Empty);
        Assert.That(result.Findings,
            Has.Some.Matches<Garethp.ModsOfMistriaInstallerLib.GmlMods.LintFinding>(finding =>
                finding.ModId == "indirect.mod" && finding.File.Length == 0
                    && finding.Message.Contains("objects created indirectly")));
    }

    [Test]
    public void ShouldNotTreatSingleQuotedTextAsObjectDeclarationEvidence()
    {
        var mod = Custom();
        mod.SetFile("gml/Monster.gml", "object_create('obj_roller', object_reserve('par_monster'), {});");

        var result = MonsterDefinitionCollector.Collect([mod], Pristine());

        Assert.That(result.Problems, Is.Empty);
        Assert.That(result.Findings,
            Has.Some.Matches<Garethp.ModsOfMistriaInstallerLib.GmlMods.LintFinding>(finding =>
                finding.ModId == "mock.mod"
                    && finding.Message.Contains("no literal object_create")));
    }

    [Test]
    public void ShouldRejectACustomMonsterWithoutAnyGmlSource()
    {
        var mod = Custom();
        mod.RemoveFile("gml/Monster.gml");

        var result = MonsterDefinitionCollector.Collect([mod], Pristine());

        Assert.That(HasProblem(result, mod, "has no gml/ source"), Is.True);
    }

    [Test]
    public void ShouldRequireAnExactAdjacentImageAndMetadataPair()
    {
        var complete = Custom(sprite: "spr_complete", id: "complete.mod");
        complete.SetFile("images/spr_complete.meta.toml", "");
        complete.SetFile("images/spr_complete.png", Array.Empty<byte>());

        var split = Custom(key: "split", category: "split", objectName: "obj_split",
            id: "split.mod", sprite: "spr_split");
        split.SetFile("images/a/spr_split.meta.toml", "");
        split.SetFile("images/b/spr_split.png", Array.Empty<byte>());

        var result = MonsterDefinitionCollector.Collect([complete, split], Pristine(sprites: new HashSet<string>()));

        Assert.That(result.Problems.Any(problem => problem.Mod == complete), Is.False);
        Assert.That(HasProblem(result, split, "no pristine sprite or this mod's staged image/meta pair"), Is.True);
    }

    [Test]
    public void ShouldRejectRepeatedNewSpritePairsWithinOneMod()
    {
        var mod = Custom(sprite: "spr_repeated");
        mod.SetFile("images/a/spr_repeated.meta.toml", "");
        mod.SetFile("images/a/spr_repeated.png", Array.Empty<byte>());
        mod.SetFile("images/b/spr_repeated.meta.toml", "");
        mod.SetFile("images/b/spr_repeated.png", Array.Empty<byte>());

        var result = MonsterDefinitionCollector.Collect([mod], Pristine(sprites: new HashSet<string>()));

        Assert.That(HasProblem(result, mod, "sprite 'spr_repeated' is staged more than once"), Is.True);
    }

    [Test]
    public void ShouldNotBorrowANewSpriteFromAnotherMod()
    {
        var owner = Custom(sprite: "spr_foreign", id: "owner.mod");
        var provider = new MockMod(new Dictionary<string, object>
        {
            ["images/spr_foreign.meta.toml"] = "",
            ["images/spr_foreign.png"] = Array.Empty<byte>(),
        }) { Id = "provider.mod" };

        var result = MonsterDefinitionCollector.Collect([owner, provider],
            Pristine(sprites: new HashSet<string>()));

        Assert.That(HasProblem(result, owner, "is supplied by mod 'provider.mod', not by the monster's mod"), Is.True);
        Assert.That(result.Problems.Any(problem => problem.Mod == provider), Is.False);
    }

    [Test]
    public void ShouldAcceptAPristineSpriteEvenWhenTheModReplacesItsPixels()
    {
        var mod = Custom();
        mod.SetFile("images/replace/spr_native.png", Array.Empty<byte>());

        var result = MonsterDefinitionCollector.Collect([mod], Pristine());

        Assert.That(result.Problems, Is.Empty);
    }

    [Test]
    public void ShouldRejectANonSpriteResource()
    {
        var mod = Custom();
        mod.SetFile("fiddle/monsters/roller.toml", Entry()
            .Replace("hurtbox = \"spr_native\"", "hurtbox = \"obj_monster_mushroom\""));

        var result = MonsterDefinitionCollector.Collect([mod], Pristine());

        Assert.That(HasProblem(result, mod, "hurtbox must name a sprite using the spr_ prefix"), Is.True);
    }

    [Test]
    public void ShouldRejectAnInvalidEastDirection()
    {
        var mod = Custom();
        mod.SetFile("fiddle/monsters/roller.toml", Entry()
            .Replace("idle = \"spr_native\"",
                "idle.north = \"spr_native\"\nidle.south = \"spr_native\"\nidle.east = 3"));

        var result = MonsterDefinitionCollector.Collect([mod], Pristine());

        Assert.That(HasProblem(result, mod, "sprites.idle.east must name a sprite"), Is.True);
    }

    [Test]
    public void ShouldApplyTheModsOwnDefaultBeforeItsMonsterTable()
    {
        var mod = Custom();
        mod.SetFile("fiddle/monsters/roller.toml",
            "[default]\ngm_object = \"obj_roller\"\naggro_radius = 192\n"
            + Entry().Replace("gm_object = \"obj_roller\"\n", "")
                .Replace("aggro_radius = 192\n", ""));

        var result = MonsterDefinitionCollector.Collect([mod], Pristine());

        Assert.That(result.Problems, Is.Empty);
        Assert.That(result.Definitions.Single().ObjectName, Is.EqualTo("obj_roller"));
    }

    [TestCase("aggro_radius")]
    [TestCase("status_effect_offset")]
    [TestCase("fire_effect_offset")]
    [TestCase("patience_acknowledgement_reset")]
    public void ShouldRequireFieldsReadDirectlyByParMonster(string field)
    {
        var mod = Custom();
        var line = Entry().Split('\n').Single(value => value.TrimStart().StartsWith(field + " ="));
        mod.SetFile("fiddle/monsters/roller.toml", Entry().Replace(line + "\n", ""));

        var result = MonsterDefinitionCollector.Collect([mod], Pristine());

        Assert.That(HasProblem(result, mod, $".{field} must be a number"), Is.True);
    }

    [TestCase("{ item = \"ore_copper\", purse = 20 }",
        "drops[0] must name exactly one supported reward")]
    [TestCase("{ animal_cosmetic = \"hat\" }",
        "drops[0].animal must be a nonempty string")]
    [TestCase("{ item = \"ore_copper\", count_range = [1] }",
        "drops[0].count_range must be a two-number array")]
    public void ShouldRejectAMalformedDrop(string drop, string problem)
    {
        var mod = Custom();
        mod.SetFile("fiddle/monsters/roller.toml",
            Entry().Replace("drops = []", $"drops = [{drop}]"));

        var result = MonsterDefinitionCollector.Collect([mod], Pristine());

        Assert.That(HasProblem(result, mod, problem), Is.True);
    }

    [Test]
    public void ShouldRejectAMalformedCategoryDeclaration()
    {
        var bad = Custom();
        bad.SetFile("momi/monster_categories/roller.toml",
            "states = [\"idle\", \"idle\"]\npreprocess = \"f\"\n");

        var result = MonsterDefinitionCollector.Collect([bad], Pristine());

        Assert.That(HasProblem(result, bad, "unknown field 'preprocess'"), Is.True);
        Assert.That(HasProblem(result, bad, "states must be distinct"), Is.True);
    }

    [Test]
    public void ShouldRejectAnUnusedCategoryDeclaration()
    {
        var unused = new MockMod(new Dictionary<string, object>
        {
            ["momi/monster_categories/unused.toml"] = "states = [\"idle\"]\n",
        }) { Id = "unused.mod" };

        var result = MonsterDefinitionCollector.Collect([unused], Pristine());

        Assert.That(HasProblem(result, unused, "has no monster definition"), Is.True);
    }

    [Test]
    public void ShouldRejectMonsterContentWithoutAVanillaRoster()
    {
        var mod = Custom();

        var result = MonsterDefinitionCollector.Collect([mod], Pristine(includeRoster: false));

        Assert.That(result.Definitions, Is.Empty);
        Assert.That(HasProblem(result, mod, "cannot read the game's MonsterId categories"), Is.True);
    }
}
