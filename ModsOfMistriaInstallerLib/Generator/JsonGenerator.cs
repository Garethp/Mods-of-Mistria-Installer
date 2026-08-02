using Garethp.ModsOfMistriaInstallerLib.Lang;
using Garethp.ModsOfMistriaInstallerLib.ModTypes;
using Newtonsoft.Json.Linq;

namespace Garethp.ModsOfMistriaInstallerLib.Generator;

[InformationGenerator(2)]
public class JsonGenerator: IGenerator
{
    private IEnumerable<string> GetFiles(IMod mod)
    {
        return mod.GetAllFiles(".json")
            .Where(p => !p.EndsWith(".meta.json", StringComparison.OrdinalIgnoreCase))
            .Select(p => RelativePath(mod, p))
            .Where(p => !p.Equals("manifest.json", StringComparison.OrdinalIgnoreCase))
            .Where(p => !p.StartsWith("points/", StringComparison.OrdinalIgnoreCase) &&
                        !p.StartsWith("points\\", StringComparison.OrdinalIgnoreCase));
    }
    
    public GeneratedInformation Generate(IMod mod)
    {
        var infomation = new GeneratedInformation();
        
        var jsonFiles = GetFiles(mod)
            .Select(file => new JsonItem
            {
                FilePath = file,
                ReadFilePath = file
            });
        
        infomation.Json.AddRange(jsonFiles);

        return infomation;
    }

    public bool CanGenerate(IMod mod) => GetFiles(mod).Any();

    public Validation Validate(IMod mod)
    {
        var validation = new Validation();
        if (!CanGenerate(mod)) return validation;

        foreach (var file in GetFiles(mod))
        {
            JObject json;
            
            try
            {
                json = JObject.Parse(mod.ReadFile(file));
            }
            catch (Exception e)
            {
                validation.AddError(mod, file, string.Format(Resources.CoreCouldNotParseFile, e.Message));
                continue;
            }

            if (json.Count == 0)
            {
                validation.AddError(mod, file, Resources.CoreNoDataInFile);
                continue;
            }
        }

        return validation;
    }

    private static string RelativePath(IMod mod, string absolutePath)
    {
        var normalizedBase = mod.GetBasePath().Replace('\\', '/').TrimEnd('/') + '/';
        var normalizedFull = absolutePath.Replace('\\', '/');
        if (normalizedFull.StartsWith(normalizedBase, StringComparison.OrdinalIgnoreCase))
            return normalizedFull[normalizedBase.Length..];
        return normalizedFull;
    }
}