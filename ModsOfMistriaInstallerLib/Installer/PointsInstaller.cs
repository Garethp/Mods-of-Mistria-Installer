using Newtonsoft.Json.Linq;

namespace Garethp.ModsOfMistriaInstallerLib.Installer;

public class PointsInstaller(): GenericInstaller(["room_data", "points"])
{
    public override List<JObject> GetNewInformation(GeneratedInformation information) => information.Points;
}