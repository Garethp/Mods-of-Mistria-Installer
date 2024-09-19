using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Garethp.ModsOfMistriaInstallerLib.Models;

[JsonObject(NamingStrategyType = typeof(SnakeCaseNamingStrategy))]
public class Conversation
{
    public string Kind;
    public string[] Requires;
    public string[] Writes;
    public Dictionary<string, ConversationLine> Lines;
    public bool MultipleSpeakersInConversation;
    public List<string> SpeakersInConversation;
    public bool CanTalkAfter;
    public string Priority;
}

[JsonObject(NamingStrategyType = typeof(SnakeCaseNamingStrategy))]
public class ConversationLine
{
    public string Local;
    public string[] Writes;
    public LineAction[] Actions;
    public NextLineBehavior NextLineBehavior;
}

[JsonObject(NamingStrategyType = typeof(SnakeCaseNamingStrategy))]
public class LineAction
{
}

public class LineActionSpeaker : LineAction
{
    public string type = "speaker";
    public string content;
}

public class LineActionPortrait : LineAction
{
    public string type = "portrait";
    public string content;
}

[JsonObject(NamingStrategyType = typeof(SnakeCaseNamingStrategy))]
public class NextLineBehavior
{
}

public class NextLineBehaviorFinish : NextLineBehavior
{
    public string type = "finish";
}

public class NextLineBehaviorNextLine : NextLineBehavior
{
    public string type = "next_line";
    public string content;
}

public class NextLineBehaviorPrompts : NextLineBehavior
{
    public string type = "prompts";
    public List<Prompt> content = [];
}

public class Prompt
{
    public string local;
    public string next_line;
}