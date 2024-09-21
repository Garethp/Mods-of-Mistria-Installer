using Newtonsoft.Json.Linq;

namespace Garethp.ModsOfMistriaInstallerLib.Generator;

[InformationGenerator(1)]
public class ConversationGenerator: IGenerator
{
    public GeneratedInformation Generate(IMod mod)
    {
        var files = mod.GetFilesInFolder("conversations");
        var generatedInformation = new GeneratedInformation();

        foreach (var file in files.Order().Where(file => file.EndsWith(".json") && !file.EndsWith(".simple.json")))
        {
            var json = JObject.Parse(mod.ReadFile(file));

            generatedInformation.Conversations.Add(json);
        }
        
        return generatedInformation;
    }

    public bool CanGenerate(IMod mod) => mod.HasFilesInFolder("conversations");

    public Validation Validate(IMod mod) => new Validation();
}