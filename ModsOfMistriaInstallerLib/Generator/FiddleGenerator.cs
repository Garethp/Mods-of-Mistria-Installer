using System.Data;
using Garethp.ModsOfMistriaInstallerLib.Models;
using Garethp.ModsOfMistriaInstallerLib.ModTypes;
using Newtonsoft.Json.Linq;

namespace Garethp.ModsOfMistriaInstallerLib.Generator;

public class FiddleGenerator() : GenericGenerator("fiddle")
{
    public override void AddJson(IMod mod, string fileName, GeneratedInformation information, JObject json)
    {
        information.Fiddles.Add(new FiddleInformation(mod.GetName(), fileName, json));
    }

    public override void AddJson(GeneratedInformation information, JObject json)
    {
        throw new NoNullAllowedException();
    }
}