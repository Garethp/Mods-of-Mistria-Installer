using Newtonsoft.Json.Linq;

namespace Garethp.ModsOfMistriaInstallerLib.Installer;

public class PlayerToolsInstaller() : GenericInstaller(["animation", "generated", "player_tools"])
{
    public override List<JObject> GetNewInformation(GeneratedInformation information) => information.PlayerTools;
}