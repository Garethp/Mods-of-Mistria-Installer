using Garethp.ModsOfMistriaInstallerLib.Utils;
using Newtonsoft.Json.Linq;

namespace Garethp.ModsOfMistriaInstallerLib.Installer;

[InformationInstaller(1)]
public class LocalisationInstaller : IModuleInstaller
{
    private IFileModifier _fileModifier = new FileModifier();

    public void SetFileModifier(IFileModifier fileModifier) => _fileModifier = fileModifier;
    
    public void Install(
        string fieldsOfMistriaLocation,
        string modsLocation,
        GeneratedInformation information,
        Action<string, string> reportStatus
    ) {
        if (_fileModifier.ConditionalRestoreBackup(
            fieldsOfMistriaLocation, 
            "localization.json", 
            () => information.Localisations.Count == 0
        )) return;

        var existingFiddle = JObject.Parse(
            _fileModifier.Read(fieldsOfMistriaLocation, "localization.json")
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

        _fileModifier.Write(
            fieldsOfMistriaLocation, 
            "localization.json",
            merged.ToString()
        );
    }
}