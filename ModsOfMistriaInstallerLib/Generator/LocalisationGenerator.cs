using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;

namespace Garethp.ModsOfMistriaInstallerLib.Generator;

[InformationGenerator(1)]
public class LocalisationGenerator: IGenerator
{
    public GeneratedInformation Generate(Mod mod)
    {
        var modLocation = mod.Location;
        var localisationFiles = new List<string>();

        if (Directory.Exists(Path.Combine(modLocation, "localisation")))
        {
            localisationFiles.AddRange(Directory.GetFiles(Path.Combine(modLocation, "localisation")));
        }
        
        if (Directory.Exists(Path.Combine(modLocation, "localization")))
        {
            localisationFiles.AddRange(Directory.GetFiles(Path.Combine(modLocation, "localization")));
        }

        var generatedInformation = new GeneratedInformation();

        foreach (var localisationFile in localisationFiles.Order().Where(file => file.EndsWith(".json")))
        {
            var languageMatch = new Regex(".*?\\.(.*?).json$").Match(Path.GetFileName(localisationFile));
            var langauge = languageMatch.Success ? languageMatch.Groups[1].Value : "eng";
            
            var localisationJson = JObject.Parse(File.ReadAllText(localisationFile));

            generatedInformation.Localisations.Add(new JObject { { langauge, localisationJson } });

        }
        
        return generatedInformation;
    }

    public bool CanGenerate(Mod mod)
    {
        return Directory.Exists(Path.Combine(mod.Location, "localisation"))
               || Directory.Exists(Path.Combine(mod.Location, "localization"));
    }
    
    public Validation Validate(Mod mod) => new Validation();
}