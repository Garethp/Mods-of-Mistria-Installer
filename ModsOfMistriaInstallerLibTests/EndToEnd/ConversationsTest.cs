using Garethp.ModsOfMistriaInstallerLib.Generator;
using Garethp.ModsOfMistriaInstallerLib.Installer;
using ModsOfMistriaInstallerLibTests.Fixtures;
using ModsOfMistriaInstallerLibTests.TestUtils;
using Newtonsoft.Json.Linq;

namespace ModsOfMistriaInstallerLibTests.EndToEnd;

[TestFixture]
public class ConversationsTest
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
    public void ShouldMergeInT2Outputs()
    {
        var mod = new MockMod(new Dictionary<string, string>
        {
            {
                "conversations/test.json", """
                                           {
                                             "Conversations/story_quests/balors_finest_wares_turn_in": {
                                               "kind": "Normal",
                                               "actions": [
                                                 {
                                                   "type": "spoke_to",
                                                   "content": "balor"
                                                 },
                                                 {
                                                   "type": "item",
                                                   "content": {
                                                     "item_id": "red_wine_bottle",
                                                     "count": 1
                                                   }
                                                 }
                                               ],
                                               "lines": {
                                                 "1": {
                                                   "local": "Conversations/story_quests/balors_finest_wares_turn_in/1",
                                                   "writes": [],
                                                   "actions": [
                                                     {
                                                       "type": "portrait",
                                                       "content": "wink"
                                                     },
                                                     {
                                                       "type": "effect",
                                                       "content": "sparkles"
                                                     }
                                                   ],
                                                   "next_line_behavior": {
                                                     "type": "next_lines",
                                                     "content": [
                                                       {
                                                         "line_id": "2",
                                                         "requirements": []
                                                       }
                                                     ]
                                                   }
                                                 },
                                                 "2": {
                                                   "local": "Conversations/story_quests/balors_finest_wares_turn_in/6",
                                                   "writes": [],
                                                   "actions": [
                                                     {
                                                       "type": "portrait",
                                                       "content": "wink"
                                                     }
                                                   ],
                                                   "next_line_behavior": {
                                                     "type": "finish"
                                                   }
                                                 },
                                                 "init": {
                                                   "local": "Conversations/story_quests/balors_finest_wares_turn_in/init",
                                                   "writes": [],
                                                   "actions": [
                                                     {
                                                       "type": "portrait",
                                                       "content": "happy"
                                                     }
                                                   ],
                                                   "next_line_behavior": {
                                                     "type": "next_lines",
                                                     "content": [
                                                       {
                                                         "line_id": "1",
                                                         "requirements": []
                                                       }
                                                     ]
                                                   }
                                                 }
                                               },
                                               "multiple_speakers_in_conversation": false,
                                               "speakers_in_conversation": [
                                                 "balor"
                                               ],
                                               "can_talk_after": false,
                                               "priority": "Max"
                                             }
                                           }
                                           """
            }
        });

        _installer.InstallMods([mod], _fileModifier);

        Assert.That(_fileModifier.GetFile("t2_output.json"), Is.Not.Matches(new MatchesJsonConstraint(new JObject())));
    }

    [Test]
    public void ShouldNotOverrideSchedulesIfEmpty()
    {
        var mod = new MockMod(new Dictionary<string, string>
        {
            { "schedule/test.json", "{ \"test\": {} }" }
        });

        _installer.InstallMods([mod], _fileModifier);
        Assert.That(_fileModifier.GetFile("t2_output.json"), Is.Not.Matches(new MatchesJsonConstraint(new JObject())));
    }
}