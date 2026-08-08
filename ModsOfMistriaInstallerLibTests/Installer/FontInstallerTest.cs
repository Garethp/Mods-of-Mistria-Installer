using Garethp.ModsOfMistriaInstallerLib.Installer;
using ModsOfMistriaInstallerLibTests.Fixtures;
using ModsOfMistriaInstallerLibTests.TestUtils;

namespace ModsOfMistriaInstallerLibTests.Installer;

[TestFixture]
public class FontInstallerTest
{
    [Test]
    public void CopiesTtfFromFontsFolderIntoAssets()
    {
        var mod = new MockMod(new Dictionary<string, object>
        {
            ["fonts/fnt_mistria_birdseed_bul.ttf"] = "font-test-payload"
        });
        var modifier = new MockFileModifier(new Dictionary<string, string>());

        Assert.That(mod.GetAllFiles(".ttf"), Is.EquivalentTo(new[] { "fonts/fnt_mistria_birdseed_bul.ttf" }));

        var reports = new List<string>();
        new FontInstaller(modifier).Install(mod, (message, _) => reports.Add(message));

        Assert.That(reports, Is.Not.Empty);
        Assert.That(modifier.Exists("assets/fonts/fnt_mistria_birdseed_bul.ttf"), Is.True);

        Assert.That(
            modifier.GetFile("assets/fonts/fnt_mistria_birdseed_bul.ttf"),
            Is.EqualTo("font-test-payload"));
    }

    [Test]
    public void IgnoresTtfOutsideFontsFolder()
    {
        var mod = new MockMod(new Dictionary<string, object>
        {
            ["images/not-a-font.ttf"] = "font-test-payload"
        });
        var modifier = new MockFileModifier(new Dictionary<string, string>());

        new FontInstaller(modifier).Install(mod, (_, _) => { });

        Assert.That(
            modifier.Exists("assets/images/not-a-font.ttf"),
            Is.False);
    }
}
