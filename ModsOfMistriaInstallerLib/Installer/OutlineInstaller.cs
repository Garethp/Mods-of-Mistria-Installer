using Newtonsoft.Json.Linq;

namespace Garethp.ModsOfMistriaInstallerLib.Installer;

public class OutlineInstaller(): GenericInstaller(["animation", "generated", "outlines"])
{
    public override List<JObject> GetNewInformation(GeneratedInformation information) => information.Outlines;
}