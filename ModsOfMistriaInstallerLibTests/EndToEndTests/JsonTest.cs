using ModsOfMistriaInstallerLibTests.Fixtures;
using ModsOfMistriaInstallerLibTests.TestUtils;
using Newtonsoft.Json.Linq;

namespace ModsOfMistriaInstallerLibTests.EndToEndTests;

[TestFixture]
public class JsonTest
{
    [Test]
    public void ShouldCreateNewJsonFiles()
    {
        var fileModifier = new MockFileModifier(new ());
        var mod = new MockMod(new Dictionary<string, object>()
        {
            {
                "file/test.json", new JObject
                {
                    { "test", "test" }
                }.ToString()
            }
        });
        
        new MockInstaller().InstallMod(mod, fileModifier);
        
        Assert.That(fileModifier.GetFile("assets/file/test.json"), new MatchesJsonConstraint(new JObject
        {
            { "test", "test" }
        }));
    }

    [Test]
    public void ShouldMergeJsonFiles()
    {
        var fileModifier = new MockFileModifier(new ()
        {
            { "assets/file/test.json", new JObject
            {
                { "test", "old" }
            }.ToString()}
        });
        var mod = new MockMod(new Dictionary<string, object>()
        {
            {
                "file/test.json", new JObject
                {
                    { "test", "new" }
                }.ToString()
            }
        });
        
        new MockInstaller().InstallMod(mod, fileModifier);
        
        Assert.That(fileModifier.GetFile("assets/file/test.json"), new MatchesJsonConstraint(new JObject
        {
            { "test", "new" }
        }));
    }
}