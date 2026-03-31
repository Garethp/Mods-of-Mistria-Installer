using Garethp.ModsOfMistriaInstallerLib.Utils;
using Newtonsoft.Json.Linq;

namespace Garethp.ModsOfMistriaInstallerLib.Installer;

[InformationInstaller(1)]
public class ConversationInstaller : IModuleInstaller
{
    private IFileModifier _fileModifier = new FileModifier();

    public void SetFileModifier(IFileModifier fileModifier) => _fileModifier = fileModifier;
    
    public void Install(
        string fieldsOfMistriaLocation,
        string modsLocation,
        GeneratedInformation information,
        Action<string, string> reportStatus
    )
    {
        

        if (_fileModifier.ConditionalRestoreBackup(
                fieldsOfMistriaLocation, 
                "t2_output.json",
                () => information.Conversations.Count == 0
            ))
        {
            return;
        };

        var existingInformation = JObject.Parse(
            _fileModifier.Read(fieldsOfMistriaLocation, "t2_output.json")
        );
        
        var allSources = new List<JObject> { existingInformation };
        allSources.AddRange(information.Conversations.Select(conversation => new JObject { ["conversations"] = conversation }));

        var merged = new JObject();
        
        foreach (var source in allSources)
        {
            merged.Merge(source, new JsonMergeSettings
            {
                MergeArrayHandling = MergeArrayHandling.Replace
            });
        }

        _fileModifier.Write(
            fieldsOfMistriaLocation, 
            "t2_output.json", 
            merged.ToString()
        );
    }
}