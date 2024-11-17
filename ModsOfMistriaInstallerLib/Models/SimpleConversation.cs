using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Garethp.ModsOfMistriaInstallerLib.Models;

[JsonObject(NamingStrategyType = typeof(CamelCaseNamingStrategy))]
public class SimpleConversation
{
    public string Name;
    public List<SimpleConversationLine> Lines;
}

[JsonObject(NamingStrategyType = typeof(CamelCaseNamingStrategy))]
public class SimpleConversationLine
{
    public string Speaker;
    public string Portrait = "neutral";
    public string Text;
    public List<SimplerConversationChoice> Choices;
}

[JsonObject(NamingStrategyType = typeof(CamelCaseNamingStrategy))]
public class SimplerConversationChoice
{
    public string Text;
    public string NextLine;
}