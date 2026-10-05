using Garethp.ModsOfMistriaInstallerLib.ModTypes;
using ModsOfMistriaInstallerLibTests.Fixtures;

namespace ModsOfMistriaInstallerLibTests.ModTypes;

// The overlay's enumeration contract, focused on the suppression the local-name
// pass relies on. A renamed file must not also enumerate under its old name.
[TestFixture]
public class GeneratedOverlayModTest
{
    [Test]
    public void ShouldHideASuppressedInnerFileFromEnumeration()
    {
        var inner = new MockMod(new Dictionary<string, object>
        {
            ["fiddle/npcs/luna.toml"] = "name = \"Luna\"\n",
            ["fiddle/npcs/other.toml"] = "name = \"Other\"\n",
        });

        var overlay = new GeneratedOverlayMod(inner,
            new Dictionary<string, string> { ["fiddle/npcs/author_mod_luna.toml"] = "name = \"Luna\"\n" },
            redirects: null,
            hidden: ["fiddle/npcs/luna.toml"]);

        var files = overlay.GetAllFiles(".toml").Select(p => p.Replace('\\', '/')).ToList();
        Assert.That(files, Does.Contain("fiddle/npcs/author_mod_luna.toml"));
        Assert.That(files, Does.Contain("fiddle/npcs/other.toml"));
        Assert.That(files, Does.Not.Contain("fiddle/npcs/luna.toml"),
            "the renamed file must not also install under its original name");
    }

    [Test]
    public void ShouldServeARenamedArtFileByRedirectAndHideTheOriginal()
    {
        var inner = new MockMod(new Dictionary<string, object>
        {
            ["animations/Luna/spr_luna_walk.png"] = "PNGDATA",
        });

        var overlay = new GeneratedOverlayMod(inner,
            new Dictionary<string, string>(),
            redirects: new Dictionary<string, string>
            {
                ["animations/Luna/spr_author_mod_luna_walk.png"] = "animations/Luna/spr_luna_walk.png",
            },
            hidden: ["animations/Luna/spr_luna_walk.png"]);

        var files = overlay.GetAllFiles(".png").Select(p => p.Replace('\\', '/')).ToList();
        Assert.That(files, Does.Contain("animations/Luna/spr_author_mod_luna_walk.png"));
        Assert.That(files, Does.Not.Contain("animations/Luna/spr_luna_walk.png"));
        // reading the renamed path returns the original file's bytes
        Assert.That(overlay.ReadFile("animations/Luna/spr_author_mod_luna_walk.png"), Is.EqualTo("PNGDATA"));
    }
}
