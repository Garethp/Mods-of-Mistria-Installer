using Garethp.ModsOfMistriaInstallerLib;
using Garethp.ModsOfMistriaInstallerLib.Collector;
using Garethp.ModsOfMistriaInstallerLib.Installer;
using ModsOfMistriaInstallerLibTests.Fixtures;
using ModsOfMistriaInstallerLibTests.TestUtils;

namespace ModsOfMistriaInstallerLibTests.Installer;

[TestFixture]
public class ManualLoadInstallerTest
{
    [Test]
    public void CopiesPngForManualLoadAnimation()
    {
        var mod = new MockMod(new Dictionary<string, string>
        {
            ["animations/UI NEW/Loading Screen/spr_loading_bird_bg.meta.toml"] = """
                [asset_properties]
                frame_size = [262, 126]
                atlas = "UI"
                tags = ["manual-load"]
                """,
            ["animations/UI NEW/Loading Screen/spr_loading_bird_bg.png"] = "png-test-payload"
        });
        var modifier = new MockFileModifier(new Dictionary<string, string>());
        var information = new TOMLCollector().Collect(mod);

        new ManualLoadInstaller(modifier).Install(mod, information, (_, _) => { });

        Assert.That(
            modifier.GetFile("assets/animations/UI NEW/Loading Screen/spr_loading_bird_bg.png"),
            Is.EqualTo("png-test-payload"));
    }

    [Test]
    public void DoesNotCopyNormalAtlasAnimationPng()
    {
        var mod = new MockMod(new Dictionary<string, string>
        {
            ["animations/Modded/spr_normal.meta.toml"] = """
                [asset_properties]
                frame_size = [16, 16]
                atlas = "UI"
                """,
            ["animations/Modded/spr_normal.png"] = "png-test-payload"
        });
        var modifier = new MockFileModifier(new Dictionary<string, string>());
        var information = new TOMLCollector().Collect(mod);

        new ManualLoadInstaller(modifier).Install(mod, information, (_, _) => { });

        Assert.That(modifier.Exists("assets/animations/Modded/spr_normal.png"), Is.False);
    }
}
