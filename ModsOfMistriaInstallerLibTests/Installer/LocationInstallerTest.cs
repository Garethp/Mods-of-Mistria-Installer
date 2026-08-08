using Garethp.ModsOfMistriaInstallerLib.Installer;
using ModsOfMistriaInstallerLibTests.Fixtures;
using ModsOfMistriaInstallerLibTests.TestUtils;

namespace ModsOfMistriaInstallerLibTests.Installer;

[TestFixture]
public class LocationInstallerTest
{
    [Test]
    public void ShouldReadLocationAndTiledFilesThroughTheModAbstraction()
    {
        var mod = new MockMod(new Dictionary<string, object>
        {
            ["momi/locations/new_room.toml"] = "[new_room]\nname = \"New Room\"\n",
            ["tiled/rooms/rm_new_room.tmx"] =
                "<property name=\"destination_id\" type=\"int\" propertytype=\"LocationId\" value=\"0\"/>"
        });
        var files = new MockFileModifier(new Dictionary<string, string>
        {
            ["assets/fiddle/locations.toml"] = "[vanilla_room]\nname = \"Vanilla Room\"\n"
        });

        new LocationInstaller("unused", files).Install([mod], (_, _) => { });

        Assert.That(files.GetFile("assets/fiddle/locations.toml"), Does.Contain("new_room"));
        Assert.That(files.GetFile("assets/tiled/rooms/rm_new_room.tmx"), Does.Contain("value=\"0\""));
    }
}
