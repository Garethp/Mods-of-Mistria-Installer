using Newtonsoft.Json.Linq;

namespace Garethp.ModsOfMistriaInstallerLib.Generator;

public class ScheduleGenerator(): GenericGenerator("schedules")
{
    public override void AddJson(GeneratedInformation information, JObject json)
    {
        information.Schedules.Add(json);
    }
}