using Newtonsoft.Json.Linq;

namespace Garethp.ModsOfMistriaInstallerLib.Generator;

[InformationGenerator(1)]
public abstract class GenericGenerator(string folderName) : IGenerator
{
    public GeneratedInformation Generate(IMod mod)
    {
        var files = mod.GetFilesInFolder(folderName);
        var generatedInformation = new GeneratedInformation();

        foreach (var file in files.Order().Where(file => file.EndsWith(".json")))
        {
            var json = JObject.Parse(mod.ReadFile(file));

            AddJson(generatedInformation, json);
        }
        
        return generatedInformation;
    }
    
    public abstract void AddJson(GeneratedInformation information, JObject json);

    public bool CanGenerate(IMod mod) => mod.HasFilesInFolder(folderName);
    
    public Validation Validate(IMod mod) => new Validation();
}