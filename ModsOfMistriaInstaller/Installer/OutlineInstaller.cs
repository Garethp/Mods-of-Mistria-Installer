using Newtonsoft.Json.Linq;

namespace Garethp.ModsOfMistriaInstaller.Installer;

public class OutlineInstaller(): GenericInstaller(["animation", "generated", "outlines"])
{
    public override List<JObject> GetNewInformation(GeneratedInformation information) => information.Outlines;
}