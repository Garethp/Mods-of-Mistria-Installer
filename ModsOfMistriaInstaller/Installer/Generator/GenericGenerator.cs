using Newtonsoft.Json.Linq;

namespace Garethp.ModsOfMistriaInstaller.Installer.Generator;

public abstract class GenericGenerator(string folderName) : IGenerator
{
    public GeneratedInformation Generate(Mod mod)
    {
        var modLocation = mod.Location;
        var files = Directory.GetFiles(Path.Combine(modLocation, folderName));
        var generatedInformation = new GeneratedInformation();

        foreach (var file in files.Order().Where(file => file.EndsWith(".json")))
        {
            var json = JObject.Parse(File.ReadAllText(file));

            AddJson(generatedInformation, json);
        }
        
        return generatedInformation;
    }
    
    public abstract void AddJson(GeneratedInformation information, JObject json);

    public bool CanGenerate(Mod mod)
    {
        return Directory.Exists(Path.Combine(mod.Location, folderName));
    }
}