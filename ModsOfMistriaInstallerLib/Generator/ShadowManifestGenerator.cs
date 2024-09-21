using Newtonsoft.Json.Linq;

namespace Garethp.ModsOfMistriaInstallerLib.Generator;

[InformationGenerator(1)]
public class ShadowManifestGenerator() : GenericGenerator("shadow_manifest")
{
    public override void AddJson(GeneratedInformation information, JObject json)
    {
        information.ShadowManifests.Add(json);
    }
}