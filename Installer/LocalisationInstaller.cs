using Newtonsoft.Json.Linq;

namespace Garethp.ModsOfMistriaInstaller.Installer;

public class LocalisationInstaller : IModuleInstaller
{
    public void Install(string fieldsOfMistriaLocation, GeneratedInformation information)
    {
        if (!File.Exists(Path.Combine(fieldsOfMistriaLocation, "localization.json")))
        {
            throw new FileNotFoundException("Could not find localization.json in Fields of Mistria folder");
        }

        if (!File.Exists(Path.Combine(fieldsOfMistriaLocation, "localization.bak.json")))
        {
            File.Copy(
                Path.Combine(fieldsOfMistriaLocation, "localization.json"),
                Path.Combine(fieldsOfMistriaLocation, "localization.bak.json")
            );
        }

        var existingFiddle = JObject.Parse(
            File.ReadAllText(Path.Combine(fieldsOfMistriaLocation, "localization.bak.json"))
        );

        var allSources = new List<JObject> { existingFiddle };

        allSources.AddRange(information.Localisations);

        var merged = new JObject();

        foreach (var source in allSources)
        {
            merged.Merge(source);
        }

        var languages = merged.Properties().Select(prop => prop.Name).ToArray();

        foreach (var language in languages)
        {
            var keys = (merged[language] as JObject)?.Properties().Select(prop => prop.Name).ToArray();
            if (keys is null) continue;

            foreach (var languageKey in keys)
            {
                foreach (var otherLanguage in languages)
                {
                    if (otherLanguage == language) continue;

                    if (merged[otherLanguage] is not JObject otherLanguageObject) continue;
                    if (otherLanguageObject.ContainsKey(languageKey)) continue;
                    
                    otherLanguageObject.Add(languageKey, "MISSING");
                }
            }
        }

        File.WriteAllText(
            Path.Combine(fieldsOfMistriaLocation, "localization.json"),
            merged.ToString()
        );
    }
}