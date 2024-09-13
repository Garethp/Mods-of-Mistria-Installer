using Newtonsoft.Json.Linq;

namespace Garethp.ModsOfMistriaInstaller.Installer.Generator;

public class PointsGenerator(): GenericGenerator("points")
{
    public override void AddJson(GeneratedInformation information, JObject json)
    {
        information.Points.Add(json);
    }
}