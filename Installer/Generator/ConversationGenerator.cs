using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;

namespace Garethp.ModsOfMistriaInstaller.Installer.Generator;

public class ConversationGenerator: IGenerator
{
    public GeneratedInformation Generate(Mod mod)
    {
        var modLocation = mod.Location;
        var files = Directory.GetFiles(Path.Combine(modLocation, "conversations"));
        var generatedInformation = new GeneratedInformation();

        foreach (var file in files.Where(file => file.EndsWith(".json") && !file.EndsWith(".simple.json")))
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
}