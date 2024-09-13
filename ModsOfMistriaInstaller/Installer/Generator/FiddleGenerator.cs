using Newtonsoft.Json.Linq;

namespace Garethp.ModsOfMistriaInstaller.Installer.Generator;

public class FiddleGenerator() : GenericGenerator("fiddle")
{
    public override void AddJson(GeneratedInformation information, JObject json)
    {
        information.Fiddles.Add(json);
    }
}