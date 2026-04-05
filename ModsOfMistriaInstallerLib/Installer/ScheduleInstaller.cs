using Garethp.ModsOfMistriaInstallerLib.Utils;
using Newtonsoft.Json.Linq;

namespace Garethp.ModsOfMistriaInstallerLib.Installer;

public class ScheduleInstaller : ISubModuleInstaller
{
    public JObject Install(JObject existingInformation, GeneratedInformation information, Action<string, string> reportStatus)
    {
        var allSources = new List<JObject> { existingInformation };
        allSources.AddRange(information.Schedules);

        var merged = new JObject();
        
        foreach (var source in allSources)
        {
            merged.Merge(source, new JsonMergeSettings
            {
                MergeArrayHandling = MergeArrayHandling.Replace
            });
        }

        return merged;
    }
}