using Garethp.ModsOfMistriaInstallerLib.Utils;
using Newtonsoft.Json.Linq;
using UndertaleModLib.Compiler;

namespace Garethp.ModsOfMistriaInstallerLib.Installer;

[InformationInstaller(1)]
public class T2Installer : IModuleInstaller
{
    private IFileModifier _fileModifier = new FileModifier();

    public void SetFileModifier(IFileModifier fileModifier) => _fileModifier = fileModifier;

    public void Install(string fieldsOfMistriaLocation, string modsLocation, GeneratedInformation information,
        Action<string, string> reportStatus)
    {
        if (_fileModifier.ConditionalRestoreBackup(
                fieldsOfMistriaLocation, 
                "t2_output.json",
                () => information.Conversations.Count == 0 && information.Schedules.Count == 0
            ))
        {
            return;
        };

        var t2Json = JObject.Parse(
            _fileModifier.Read(fieldsOfMistriaLocation, "t2_output.json")
        );
        
        if (t2Json.SelectToken("$.schedules") is not JObject schedules)
            return;

        if (t2Json.SelectToken("$.conversations") is not JObject conversations)
            return;
        
        t2Json["schedules"] = new ScheduleInstaller().Install(schedules, information, reportStatus);
        t2Json["conversations"] =  new ConversationInstaller().Install(conversations, information, reportStatus);
    
        _fileModifier.Write(fieldsOfMistriaLocation, "t2_output.json", t2Json.ToString());
    }
}