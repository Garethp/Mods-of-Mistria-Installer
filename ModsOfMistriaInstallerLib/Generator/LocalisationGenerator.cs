using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;

namespace Garethp.ModsOfMistriaInstallerLib.Generator;

[InformationGenerator(1)]
public class LocalisationGenerator: IGenerator
{
    public GeneratedInformation Generate(IMod mod)
    {
        var localisationFiles = new List<string>();
        
        localisationFiles.AddRange(mod.GetFilesInFolder("localisation"));
        localisationFiles.AddRange(mod.GetFilesInFolder("localization"));

        var generatedInformation = new GeneratedInformation();

        foreach (var localisationFile in localisationFiles.Order().Where(file => file.EndsWith(".json")))
        {
            var languageMatch = new Regex(".*?\\.(.*?).json$").Match(Path.GetFileName(localisationFile));
            var langauge = languageMatch.Success ? languageMatch.Groups[1].Value : "eng";
            
            var localisationJson = JObject.Parse(mod.ReadFile(localisationFile));

            generatedInformation.Localisations.Add(new JObject { { langauge, localisationJson } });

        }
        
        return generatedInformation;
    }

    public bool CanGenerate(IMod mod) => mod.HasFilesInFolder("localisation") || mod.HasFilesInFolder("localization");
    
    public Validation Validate(IMod mod) => new Validation();
}