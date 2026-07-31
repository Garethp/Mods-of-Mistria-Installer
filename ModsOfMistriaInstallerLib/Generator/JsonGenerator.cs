using Garethp.ModsOfMistriaInstallerLib.ModTypes;

namespace Garethp.ModsOfMistriaInstallerLib.Generator;

public class JsonGenerator
{
    public GeneratedInformation Generate(IMod mod)
    {
        var infomation = new GeneratedInformation();
        
        var jsonFiles = mod.GetAllFiles(".json")
            .Where(p => !p.EndsWith(".meta.json", StringComparison.OrdinalIgnoreCase))
            .Select(p => RelativePath(mod, p))
            .Where(p => !p.Equals("manifest.json", StringComparison.OrdinalIgnoreCase))
            .Where(p => !p.StartsWith("points/", StringComparison.OrdinalIgnoreCase) &&
                        !p.StartsWith("points\\", StringComparison.OrdinalIgnoreCase))
            .Select(file => new JsonItem
            {
                FilePath = file,
                ReadFilePath = file
            });
        
        infomation.Json.AddRange(jsonFiles);

        return infomation;
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