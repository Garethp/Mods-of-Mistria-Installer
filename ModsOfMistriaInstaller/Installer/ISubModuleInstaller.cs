using Newtonsoft.Json.Linq;

namespace Garethp.ModsOfMistriaInstaller.Installer;

public interface ISubModuleInstaller
{
    public JObject Install(JObject existingInformation, GeneratedInformation information, Action<string, string> reportStatus);
}