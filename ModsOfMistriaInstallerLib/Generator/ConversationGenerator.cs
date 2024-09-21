﻿using Newtonsoft.Json.Linq;

namespace Garethp.ModsOfMistriaInstallerLib.Generator;

[InformationGenerator(1)]
public class ConversationGenerator: IGenerator
{
    public GeneratedInformation Generate(Mod mod)
    {
        var modLocation = mod.Location;
        var files = Directory.GetFiles(Path.Combine(modLocation, "conversations"));
        var generatedInformation = new GeneratedInformation();

        foreach (var file in files.Order().Where(file => file.EndsWith(".json") && !file.EndsWith(".simple.json")))
        {
            var json = JObject.Parse(File.ReadAllText(file));

            generatedInformation.Conversations.Add(json);
        }
        
        return generatedInformation;
    }

    public bool CanGenerate(Mod mod)
    {
        return Directory.Exists(Path.Combine(mod.Location, "conversations"));
    }

    public Validation Validate(Mod mod) => new Validation();
}