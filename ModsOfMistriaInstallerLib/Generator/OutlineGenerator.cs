using Newtonsoft.Json.Linq;

namespace Garethp.ModsOfMistriaInstallerLib.Generator;

[InformationGenerator(1)]
public class OutlineGenerator(): GenericGenerator("outlines")
{
    public override void AddJson(GeneratedInformation information, JObject json) =>
        information.Outlines.Add(json);
}