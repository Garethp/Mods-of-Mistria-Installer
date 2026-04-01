using Garethp.ModsOfMistriaInstallerLib.Utils;
using Newtonsoft.Json.Linq;

namespace Garethp.ModsOfMistriaInstallerLib.Installer;

[InformationInstaller(1)]
public class MacroLinesInstaller : IModuleInstaller
{
    private IFileModifier _fileModifier = new FileModifier();

    public void SetFileModifier(IFileModifier fileModifier) => _fileModifier = fileModifier;

    public void Install(string fieldsOfMistriaLocation, string modsLocation, GeneratedInformation information,
        Action<string, string> reportStatus)
    {
        var macroLines = new JObject();

        List<string> macroKeys = ["@he", "@she", "@they", "@it", "@none"];
        
        foreach (var localisation in information.Localisations)
        {
            foreach (var languageObj in localisation.Properties())
            {
                var language = languageObj.Name;
                var lines = languageObj.Value;

                if (lines is not JObject linesObj) continue;

                var keys = linesObj.Properties()
                    .Select(property => property.Name)
                    .Where(key => macroKeys.Any(key.EndsWith))
                    .Select(key =>
                    {
                        macroKeys.ForEach(macro =>
                        {
                            if (key.EndsWith(macro))
                            {
                                key = key.Replace(macro, "");
                            }
                        });

                        return key;
                    })
                    .ToList();

                if (keys.Count == 0) continue;
                if (!macroLines.ContainsKey(language))
                {
                    macroLines.Add(language, new JArray());
                }

                foreach (var key in keys)
                {
                    (macroLines[language] as JArray)!.Add(key);
                }
            }
        }

        if (_fileModifier.ConditionalRestoreBackup(
                fieldsOfMistriaLocation,
                "macro_lines.json",
                () => macroLines.Count == 0
            )) return;

        var source = JObject.Parse(
            _fileModifier.Read(fieldsOfMistriaLocation, "macro_lines.json")
        );

        var merged = new JObject();
        
        merged.Merge(source, new JsonMergeSettings
        {
            MergeArrayHandling = MergeArrayHandling.Concat
        });

        merged.Merge(macroLines, new JsonMergeSettings
        {
            MergeArrayHandling = MergeArrayHandling.Concat
        });

        foreach (var langaugeObj in merged.Properties())
        {
            if (langaugeObj.Value is not JArray lines) continue;

            langaugeObj.Value = new JArray(lines.Distinct());
        }
        
        _fileModifier.Write(fieldsOfMistriaLocation, "macro_lines.json", merged.ToString());
    }
}