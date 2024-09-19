using Newtonsoft.Json.Linq;

namespace Garethp.ModsOfMistriaInstallerLib.Generator;

public class PointsGenerator(): GenericGenerator("points")
{
    public override void AddJson(GeneratedInformation information, JObject json)
    {
        information.Points.Add(json);
    }
}