using Garethp.ModsOfMistriaInstallerLib.Generator;
using Garethp.ModsOfMistriaInstallerLib.Seam;
using ModsOfMistriaInstallerLibTests.Fixtures;

namespace ModsOfMistriaInstallerLibTests.Generator;

// The install-time pass that rewrites a mod's npc_roster local name (luna)
// to the derived symbol (author_mod_luna) across its own content files.
[TestFixture]
public class ExtensionLocalNamesTest
{
    private static ExtensionRegistration Reg(string local = "luna", string symbol = "author_mod_luna") =>
        new("npc_roster", symbol, local, "author.mod",
            new Dictionary<string, string> { ["object"] = $"obj_{symbol}" });

    private static (Dictionary<string, string> Generated,
        Dictionary<string, string> Redirects,
        HashSet<string> Hidden) Expand(Dictionary<string, string> files,
        params ExtensionRegistration[] regs)
    {
        var mod = new MockMod(files.ToDictionary(kv => kv.Key, kv => (object)kv.Value)) { Id = "author.mod" };
        Dictionary<string, string> generated = new();
        Dictionary<string, string> redirects = new(StringComparer.OrdinalIgnoreCase);
        HashSet<string> hidden = new(StringComparer.OrdinalIgnoreCase);
        ExtensionLocalNames.Expand(mod, regs, generated, redirects, hidden);
        return (generated, redirects, hidden);
    }

    [Test]
    public void ShouldRenameTheFiddleFileAndHideTheLocalOne()
    {
        var (generated, _, hidden) = Expand(new Dictionary<string, string>
        {
            ["fiddle/npcs/luna.toml"] = "name = \"Luna\"\noutfits = [\"spring\"]\n",
        }, Reg());

        Assert.That(generated.ContainsKey("fiddle/npcs/author_mod_luna.toml"), Is.True);
        Assert.That(hidden, Does.Contain("fiddle/npcs/luna.toml"));
        // display text is untouched, because prose "Luna" is not a lowercase token run
        Assert.That(generated["fiddle/npcs/author_mod_luna.toml"], Does.Contain("name = \"Luna\""));
    }

    [Test]
    public void ShouldRewriteCycleSpriteNameStringsInsideTheFiddle()
    {
        var (generated, _, _) = Expand(new Dictionary<string, string>
        {
            ["fiddle/npcs/luna.toml"] = "portrait = \"spr_portrait_luna_happy\"\n",
        }, Reg());

        Assert.That(generated["fiddle/npcs/author_mod_luna.toml"],
            Does.Contain("spr_portrait_author_mod_luna_happy"));
    }

    [Test]
    public void ShouldRewriteLetterSenderValuesInFiddleContent()
    {
        var (generated, _, _) = Expand(new Dictionary<string, string>
        {
            ["fiddle/letters.toml"] = "[luna_hello_letter]\nnpc = \"luna\"\nsubject_line = \"hi\"\n",
        }, Reg());

        Assert.That(generated["fiddle/letters.toml"], Does.Contain("npc = \"author_mod_luna\""));
        // the letter key is the author's own name and stays as written
        Assert.That(generated["fiddle/letters.toml"], Does.Contain("[luna_hello_letter]"));
    }

    [Test]
    public void ShouldLeaveAFullSymbolFiddleUntouched()
    {
        var (generated, _, hidden) = Expand(new Dictionary<string, string>
        {
            ["fiddle/npcs/author_mod_luna.toml"] = "name = \"Luna\"\n",
        }, Reg());

        Assert.That(generated, Is.Empty);
        Assert.That(hidden, Is.Empty);
    }

    [Test]
    public void ShouldRewriteScheduleTableHeadersAndDottedKeys()
    {
        var (generated, _, _) = Expand(new Dictionary<string, string>
        {
            ["t2/Schedules/Luna Schedules/luna_spring.s.toml"] =
                "[luna.\"6:00am\"]\ndestination = \"town/Flower Beds\"\n",
            ["t2/Schedules/basement_schedule.s.toml"] =
                "luna.\"6:00am\" = \"aldaria/default\"\n",
        }, Reg());

        Assert.That(generated["t2/Schedules/Luna Schedules/luna_spring.s.toml"],
            Does.Contain("[author_mod_luna.\"6:00am\"]"));
        Assert.That(generated["t2/Schedules/basement_schedule.s.toml"],
            Does.StartWith("author_mod_luna.\"6:00am\""));
    }

    [Test]
    public void ShouldRewriteDialogueNpcConditionsAndLeaveTheKeyAndProse()
    {
        var (generated, _, _) = Expand(new Dictionary<string, string>
        {
            ["t2/Conversations/Bank/Luna/Banked Lines/luna_hello.c.toml"] =
                "[luna_hello]\nrequires = [{ npc = \"luna\" }]\nlocal = \"Hello from Luna.\"\n",
        }, Reg());

        var text = generated["t2/Conversations/Bank/Luna/Banked Lines/luna_hello.c.toml"];
        // the npc condition is a real symbol reference and is rewritten
        Assert.That(text, Does.Contain("npc = \"author_mod_luna\""));
        // the banked-line key is an author identifier namespaced by its path,
        // not a symbol the engine resolves, so it is left alone
        Assert.That(text, Does.Contain("[luna_hello]"));
        Assert.That(text, Does.Contain("Hello from Luna."), "prose is untouched");
    }

    [Test]
    public void ShouldRenameArtFilesByRedirectAndHideTheLocalName()
    {
        var (_, redirects, hidden) = Expand(new Dictionary<string, string>
        {
            ["animations/NPCs/Luna/spr_luna_walk_down.png"] = "PNGDATA",
            ["animations/NPCs/Luna/spr_luna_walk_down.meta.toml"] = "frame_len = 4\n",
            ["shapes/NPCs/Luna/poly_luna_body.meta.toml"] = "kind = \"box\"\n",
        }, Reg());

        Assert.That(redirects.ContainsKey("animations/NPCs/Luna/spr_author_mod_luna_walk_down.png"), Is.True);
        Assert.That(redirects["animations/NPCs/Luna/spr_author_mod_luna_walk_down.png"],
            Is.EqualTo("animations/NPCs/Luna/spr_luna_walk_down.png"));
        Assert.That(redirects.ContainsKey("shapes/NPCs/Luna/poly_author_mod_luna_body.meta.toml"), Is.True);
        Assert.That(hidden, Does.Contain("animations/NPCs/Luna/spr_luna_walk_down.png"));
    }

    [Test]
    public void ShouldMatchWholeSegmentsNotSubstrings()
    {
        // `luna` matches the segment in spr_luna_walk, but never the substring
        // inside `lunar` (one segment, not equal to `luna`)
        Assert.That(ExtensionLocalNames.RewriteIdentifier("spr_lunar_moth",
            [(new[] { "luna" }, "luna", "author_mod_luna")]), Is.EqualTo("spr_lunar_moth"));
        Assert.That(ExtensionLocalNames.RewriteIdentifier("spr_luna_walk",
            [(new[] { "luna" }, "luna", "author_mod_luna")]), Is.EqualTo("spr_author_mod_luna_walk"));
        // a `luna` segment followed by more is still the NPC, because the author named
        // it luna and every luna-segment in their files is that NPC
        Assert.That(ExtensionLocalNames.RewriteIdentifier("spr_luna_two_moth",
            [(new[] { "luna" }, "luna", "author_mod_luna")]), Is.EqualTo("spr_author_mod_luna_two_moth"));
    }

    [Test]
    public void ShouldNotRecurseWhenTheSymbolContainsTheLocalName()
    {
        // the symbol author_mod_luna contains the segment `luna`. A naive
        // rescan would rewrite it again. One pass, replacements emitted whole.
        var result = ExtensionLocalNames.RewriteIdentifier("luna",
            [(new[] { "luna" }, "luna", "author_mod_luna")]);
        Assert.That(result, Is.EqualTo("author_mod_luna"));
    }

    [Test]
    public void ShouldLeaveANameAlreadyCarryingTheFullSymbolUntouched()
    {
        // The live regression this pins. The fixture shipped full-symbol art
        // names, and the local segment inside them re-expanded to a
        // double-prefixed name that every declared portrait then fatally
        // missed. A run spelling the full symbol is consumed whole.
        var map = new List<(string[], string, string)>
        {
            (new[] { "echo" }, "echo", "momitest_extfixture_echo"),
        };
        Assert.That(ExtensionLocalNames.RewriteIdentifier(
                "spr_portrait_momitest_extfixture_echo_winter_embarrassed", map),
            Is.EqualTo("spr_portrait_momitest_extfixture_echo_winter_embarrassed"));

        // and the pass is idempotent. Rewriting its own output changes nothing
        var once = ExtensionLocalNames.RewriteIdentifier("spr_echo_walk_south", map);
        Assert.That(once, Is.EqualTo("spr_momitest_extfixture_echo_walk_south"));
        Assert.That(ExtensionLocalNames.RewriteIdentifier(once, map), Is.EqualTo(once));
    }

    [Test]
    public void ShouldNotRedirectArtFilesAlreadyNamedWithTheFullSymbol()
    {
        var (_, redirects, hidden) = Expand(new Dictionary<string, string>
        {
            ["animations/NPCs/Luna/spr_author_mod_luna_walk_down.png"] = "PNGDATA",
            ["animations/NPCs/Luna/spr_author_mod_luna_walk_down.meta.toml"] = "frame_len = 4\n",
        }, Reg());

        Assert.That(redirects, Is.Empty);
        Assert.That(hidden, Is.Empty);
    }

    [Test]
    public void ShouldPreferTheLongerLocalNameWhenTwoOverlap()
    {
        // two registrants, luna and luna_two. The segment run luna_two must
        // resolve to its own symbol, not luna's symbol plus a stray _two
        var map = new List<(string[], string, string)>
        {
            (new[] { "luna", "two" }, "luna_two", "author_mod_luna_two"),
            (new[] { "luna" }, "luna", "author_mod_luna"),
        };
        Assert.That(ExtensionLocalNames.RewriteIdentifier("spr_luna_two_walk", map),
            Is.EqualTo("spr_author_mod_luna_two_walk"));
        Assert.That(ExtensionLocalNames.RewriteIdentifier("spr_luna_walk", map),
            Is.EqualTo("spr_author_mod_luna_walk"));
    }

    [Test]
    public void ShouldIgnoreNonNpcRosterRegistrations()
    {
        var (generated, redirects, hidden) = Expand(new Dictionary<string, string>
        {
            ["fiddle/npcs/luna.toml"] = "name = \"Luna\"\n",
        }, new ExtensionRegistration("status_effect", "author_mod_luna", "luna", "author.mod",
            new Dictionary<string, string>()));

        Assert.That(generated, Is.Empty);
        Assert.That(redirects, Is.Empty);
        Assert.That(hidden, Is.Empty);
    }

    // The pass and the overlay against a real FolderMod on disk. FolderMod
    // returns absolute paths with Windows separators where MockMod returns
    // relative keys, and that difference is exactly where a path filter can
    // pass its unit test and find nothing in practice.
    [Test]
    public void ShouldExpandAndSuppressThroughARealFolderMod()
    {
        var root = Path.Combine(Path.GetTempPath(), "momi_localnames_" + Path.GetRandomFileName());
        try
        {
            Directory.CreateDirectory(Path.Combine(root, "fiddle", "npcs"));
            Directory.CreateDirectory(Path.Combine(root, "t2", "Schedules"));
            Directory.CreateDirectory(Path.Combine(root, "animations", "Luna"));
            File.WriteAllText(Path.Combine(root, "manifest.json"), """
                {
                  "name": "mod",
                  "author": "author",
                  "version": "1.0.0",
                  "minInstallerVersion": "0.12"
                }
                """);
            File.WriteAllText(Path.Combine(root, "fiddle", "npcs", "luna.toml"),
                "name = \"Luna\"\nportrait = \"spr_portrait_luna_happy\"\n");
            File.WriteAllText(Path.Combine(root, "t2", "Schedules", "luna_spring.s.toml"),
                "[luna.\"6:00am\"]\ndestination = \"town/Flower Beds\"\n");
            File.WriteAllText(Path.Combine(root, "animations", "Luna", "spr_luna_walk.png"),
                "PNGDATA");
            File.WriteAllText(Path.Combine(root, "animations", "Luna", "spr_luna_walk.meta.toml"),
                "frame_len = 4\n");

            var inner = Garethp.ModsOfMistriaInstallerLib.ModTypes.FolderMod.FromManifest(root)!;
            Dictionary<string, string> generated = new();
            Dictionary<string, string> redirects = new(StringComparer.OrdinalIgnoreCase);
            HashSet<string> hidden = new(StringComparer.OrdinalIgnoreCase);
            ExtensionLocalNames.Expand(inner, [Reg()], generated, redirects, hidden);

            Assert.That(generated.Keys, Does.Contain("fiddle/npcs/author_mod_luna.toml"));
            Assert.That(generated["fiddle/npcs/author_mod_luna.toml"],
                Does.Contain("spr_portrait_author_mod_luna_happy"));
            Assert.That(generated["t2/Schedules/luna_spring.s.toml"],
                Does.Contain("[author_mod_luna.\"6:00am\"]"));
            Assert.That(redirects.Keys, Does.Contain("animations/Luna/spr_author_mod_luna_walk.png"));

            var overlay = new Garethp.ModsOfMistriaInstallerLib.ModTypes.GeneratedOverlayMod(
                inner, generated, redirects, hidden);
            var tomls = overlay.GetAllFiles(".toml").Select(Rel).ToList();
            var pngs = overlay.GetAllFiles(".png").Select(Rel).ToList();

            Assert.That(tomls, Does.Contain("fiddle/npcs/author_mod_luna.toml"));
            Assert.That(tomls, Does.Not.Contain("fiddle/npcs/luna.toml"),
                "the renamed fiddle must not also install under its local name");
            Assert.That(pngs, Does.Contain("animations/Luna/spr_author_mod_luna_walk.png"));
            Assert.That(pngs, Does.Not.Contain("animations/Luna/spr_luna_walk.png"));
            Assert.That(overlay.ReadFile("animations/Luna/spr_author_mod_luna_walk.png"),
                Is.EqualTo("PNGDATA"));

            string Rel(string path)
            {
                var normalized = path.Replace('\\', '/');
                var basePath = root.Replace('\\', '/').TrimEnd('/');
                return normalized.StartsWith(basePath + "/", StringComparison.OrdinalIgnoreCase)
                    ? normalized[(basePath.Length + 1)..]
                    : normalized;
            }
        }
        finally
        {
            Directory.Delete(root, true);
        }
    }
}
