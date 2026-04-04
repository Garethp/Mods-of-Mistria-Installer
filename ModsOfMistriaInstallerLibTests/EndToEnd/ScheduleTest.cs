using Garethp.ModsOfMistriaInstallerLib.Generator;
using Garethp.ModsOfMistriaInstallerLib.Installer;
using ModsOfMistriaInstallerLibTests.Fixtures;
using ModsOfMistriaInstallerLibTests.Utils;
using Newtonsoft.Json.Linq;

namespace ModsOfMistriaInstallerLibTests.EndToEnd;

[TestFixture]
public class ScheduleTest
{
    private MockFileModifier _fileModifier;

    private readonly MockInstaller _installer = new([
        new ConversationGenerator(),
        new ScheduleGenerator()
    ], [
        new ConversationInstaller(),
        new ScheduleInstaller()
    ]);

    [SetUp]
    public void SetUp()
    {
        _fileModifier = new(new Dictionary<string, string>
        {
            { "t2_output.json", "{}" }
        });
    }

    [Test]
    public void ShouldNotOverrideConversationsIfEmpty()
    {
        var mod = new MockMod(new Dictionary<string, string>
        {
            { "conversations/test.json", "{ \"test\": {} }" }
        });

        _installer.InstallMods([mod], _fileModifier);
        Assert.That(_fileModifier.GetFile("t2_output.json"), Is.Not.Matches(new MatchesJsonConstraint(new JObject())));
    }
}