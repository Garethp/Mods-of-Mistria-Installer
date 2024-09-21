using Newtonsoft.Json.Linq;

namespace Garethp.ModsOfMistriaInstallerLib.Installer;

[InformationInstaller(1)]
public class ShadowManifestInstaller(): GenericInstaller(["animation", "generated", "shadow_manifest"])
{
    public override List<JObject> GetNewInformation(GeneratedInformation information) => information.ShadowManifests;
}