using Newtonsoft.Json.Linq;

namespace Garethp.ModsOfMistriaInstaller.Installer.Generator;

public abstract class GenericGenerator(string folderName) : IGenerator
{
    public GeneratedInformation Generate(string modLocation)
    {
        var files = Directory.GetFiles(Path.Combine(modLocation, folderName));
        var generatedInformation = new GeneratedInformation();

        foreach (var file in files.Where(file => file.EndsWith(".json")))
        {
            var json = JObject.Parse(File.ReadAllText(file));

            AddJson(generatedInformation, json);
        }
        
        return generatedInformation;
    }
    
    public abstract void AddJson(GeneratedInformation information, JObject json);

    public bool CanGenerate(string modLocation)
    {
        return Directory.Exists(Path.Combine(modLocation, folderName));
    }
}