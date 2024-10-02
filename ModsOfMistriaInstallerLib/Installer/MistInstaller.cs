using Newtonsoft.Json.Linq;

namespace Garethp.ModsOfMistriaInstallerLib.Installer;

[InformationInstaller(1)]
public class MistInstaller(): GenericInstaller(["__mist__"])
{
    public override List<JObject> GetNewInformation(GeneratedInformation information) => information.Cutscenes;
}