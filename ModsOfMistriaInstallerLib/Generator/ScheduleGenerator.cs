using Newtonsoft.Json.Linq;

namespace Garethp.ModsOfMistriaInstallerLib.Generator;

public class ScheduleGenerator(): GenericGenerator("schedule")
{
    public override void AddJson(GeneratedInformation information, JObject json)
    {
        information.Schedules.Add(json);
    }
}