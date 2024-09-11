﻿using Newtonsoft.Json.Linq;

namespace Garethp.ModsOfMistriaInstaller.Installer;

public class ConversationInstaller : IModuleInstaller
{
    public void Install(string fieldsOfMistriaLocation, GeneratedInformation information)
    {
        if (!File.Exists(Path.Combine(fieldsOfMistriaLocation, "t2_output.json")))
        {
            throw new FileNotFoundException("Could not find t2_output.json in Fields of Mistria folder");
        }

        if (!File.Exists(Path.Combine(fieldsOfMistriaLocation, "t2_output.bak.json")))
        {
            File.Copy(
                Path.Combine(fieldsOfMistriaLocation, "t2_output.json"),
                Path.Combine(fieldsOfMistriaLocation, "t2_output.bak.json")
            );
        }
        
        var existingInformation = JObject.Parse(
            File.ReadAllText(Path.Combine(fieldsOfMistriaLocation, "t2_output.bak.json"))
        );
        
        var allSources = new List<JObject> { existingInformation };
        allSources.AddRange(information.Conversations.Select(conversation => new JObject { ["conversations"] = conversation }));

        var merged = new JObject();
        
        foreach (var source in allSources)
        {
            merged.Merge(source);
        }

        File.WriteAllText(
            Path.Combine(fieldsOfMistriaLocation, "t2_output.json"),
            merged.ToString()
        );
    }
}