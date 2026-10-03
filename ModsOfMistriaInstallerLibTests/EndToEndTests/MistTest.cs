using ModsOfMistriaInstallerLibTests.Fixtures;
using ModsOfMistriaInstallerLibTests.TestUtils;

namespace ModsOfMistriaInstallerLibTests.EndToEndTests;

[TestFixture]
public class MistTest
{
    private MockMod GetMod()
    {
        return new MockMod(new Dictionary<string, object>
        {
            {
                "mist_scripts/test.mist",
                """
                fun on_scene_end() {}
                
                free fade_in(2);
                next_line();
                next_line();
                """
            }
        });
    }

    [Test]
    public void ShouldInstallMist()
    {
        var fileModifier = new MockFileModifier(new ());
        
        var mod = GetMod();
        
        new MockInstaller().InstallMod(mod, fileModifier);

        Assert.That(fileModifier.GetFile("assets/mist_scripts/test.mist"),
            Is.EqualTo(mod.ReadFile("mist_scripts/test.mist")));
        Assert.That(
            fileModifier.GetFile("assets/mist_scripts/test.meta.toml"),
            new ContainsTomlConstraint("""
                                       [meta_properties]
                                       asset_kind = "Mist"
                                       """)
        );
    }
    
    [Test]
    public void ShouldOverwriteExistingFile()
    {
        var fileModifier = new MockFileModifier(new Dictionary<string, string>()
        {
            { "assets/mist_scripts/test.mist", "testing" }
        });
        
        var mod = GetMod();
        
        new MockInstaller().InstallMod(mod, fileModifier);

        Assert.That(fileModifier.GetFile("assets/mist_scripts/test.mist"),
            Is.EqualTo(mod.ReadFile("mist_scripts/test.mist")));
    }
}