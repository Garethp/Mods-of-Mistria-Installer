using Newtonsoft.Json.Linq;

namespace Garethp.ModsOfMistriaInstallerLib.Installer;

public class ScriptsInstaller(): GenericInstaller(["__mist__"])
{
    public override List<JObject> GetNewInformation(GeneratedInformation information) => information.Scripts;
}