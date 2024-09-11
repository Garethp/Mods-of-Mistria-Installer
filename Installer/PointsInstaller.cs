using Newtonsoft.Json.Linq;

namespace Garethp.ModsOfMistriaInstaller.Installer;

public class PointsInstaller(): GenericInstaller(["room_data", "points"])
{
    public override List<JObject> GetNewInformation(GeneratedInformation information) => information.Points;
}