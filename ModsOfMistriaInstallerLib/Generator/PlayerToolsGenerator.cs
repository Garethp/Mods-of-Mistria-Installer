using Newtonsoft.Json.Linq;

namespace Garethp.ModsOfMistriaInstallerLib.Generator;

[InformationGenerator(1)]
public class PlayerToolsGenerator(): GenericGenerator("player_tools")
{
    public override void AddJson(GeneratedInformation information, JObject json) => information.PlayerTools.Add(json);
}