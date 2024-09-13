using Newtonsoft.Json.Linq;

namespace Garethp.ModsOfMistriaInstaller.Installer;

public class ScriptsInstaller(): GenericInstaller(["__mist__"])
{
    public override List<JObject> GetNewInformation(GeneratedInformation information) => information.Scripts;
}