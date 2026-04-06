using Garethp.ModsOfMistriaInstallerLib.Generator;
using Garethp.ModsOfMistriaInstallerLib.Installer;
using ModsOfMistriaInstallerLibTests.Fixtures;
using ModsOfMistriaInstallerLibTests.TestUtils;
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
        new T2Installer()
    ]);

    [SetUp]
    public void SetUp()
    {
        _fileModifier = new(new Dictionary<string, string>
        {
            { "t2_output.json", new JObject
            {
                { "conversations", new JObject() },
                { "schedules", new JObject() },
            }.ToString() }
        });
    }

    [Test]
    public void ShouldMergeScheduleChanges()
    {
        var mod = new MockMod(new Dictionary<string, string>
        {
            { "schedules/test.json", new JObject()
            {
                { "balor", new JObject
                {
                    { "monday", new JObject() }
                }}
            }.ToString() }
        });

        _installer.InstallMods([mod], _fileModifier);
        Assert.That(_fileModifier.GetFile("t2_output.json"), new ContainsJsonConstraint(new JObject
        {
            { "schedules", new JObject()
            {
                { "balor", new JObject
                {
                    { "monday", new JObject() }
                }}
            } },
        }));
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