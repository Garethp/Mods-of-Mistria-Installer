using Garethp.ModsOfMistriaInstallerLib.Installer;
using ModsOfMistriaInstallerLibTests.Fixtures;
using ModsOfMistriaInstallerLibTests.TestUtils;

namespace ModsOfMistriaInstallerLibTests.Installer;

[TestFixture]
public class TOMLInstallerTest
{
    [Test]
    public void ShouldFoldARetiredAtlasCategoryInTheInstalledMeta()
    {
        var installed = InstallAnimationMeta("""
            [asset_properties]
            frame_size = [8, 8]
            atlas = "Shadow"
            """);

        Assert.That(installed, Does.Contain("atlas = \"Default\""));
        Assert.That(installed, Does.Not.Contain("Shadow"));
    }

    [Test]
    public void ShouldKeepACustomAtlasCategory()
    {
        var installed = InstallAnimationMeta("""
            [asset_properties]
            frame_size = [8, 8]
            atlas = "DeepDungeonWorld"
            """);

        Assert.That(installed, Does.Contain("atlas = \"DeepDungeonWorld\""));
    }

    // Installs one animation meta and returns the text written into assets/
    private static string InstallAnimationMeta(string metaToml)
    {
        var mod = new MockMod(new Dictionary<string, string>
        {
            { "animations/Modded/spr_test_thing.meta.toml", metaToml },
        });
        var modifier = new MockFileModifier(new Dictionary<string, string>());

        new TOMLInstaller(new Dictionary<string, string>(), modifier).Install(mod, (_, _) => { });

        return modifier.GetFile(Path.Combine("assets", "animations", "Modded", "spr_test_thing.meta.toml"));
    }
}
