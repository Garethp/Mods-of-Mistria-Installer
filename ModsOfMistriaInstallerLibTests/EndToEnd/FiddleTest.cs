using Garethp.ModsOfMistriaInstallerLib.Generator;
using Garethp.ModsOfMistriaInstallerLib.Installer;
using Garethp.ModsOfMistriaInstallerLib.ModTypes;
using ModsOfMistriaInstallerLibTests.Fixtures;
using ModsOfMistriaInstallerLibTests.Utils;
using Newtonsoft.Json.Linq;

namespace ModsOfMistriaInstallerLibTests.EndToEnd;

[TestFixture]
public class FiddleTest
{
    [Test]
    public void ShouldEnsureExtraObjectsAndItems()
    {
        var moddedFile = new JObject();
        
        var mod = new MockMod(new Dictionary<string, string>()
        {
            { "fiddle/fiddle.json", moddedFile.ToString() }
        });
     
        var originalFiddle = new JObject();
        
        var fileModifier = new MockFileModifier(new Dictionary<string, string>()
        {
            { "__fiddle__.json", originalFiddle.ToString() }
        });
        
        new MockInstaller([new FiddleGenerator()], [new FiddleInstaller()])
            .InstallMods([mod], fileModifier);
        
        Assert.That(fileModifier.GetFile("__fiddle__.json"), Is.EqualTo(new JObject()
        {
            { 
                "extras", new JObject()
                {
                    { "objects", new JArray() },
                    { "items", new JArray() }
                }
            },
            { "extras/objects", new JArray() },
            { "extras/items", new JArray() }
        }.ToString()));
    }
}