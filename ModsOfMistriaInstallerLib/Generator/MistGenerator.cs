using Garethp.ModsOfMistriaInstallerLib.ModTypes;

namespace Garethp.ModsOfMistriaInstallerLib.Generator;

[InformationGenerator(2)]
public class MistGenerator: IGenerator
{
    public GeneratedInformation Generate(IMod mod)
    {
        var information = new GeneratedInformation();

        var files = mod
            .GetAllFiles(".mist")
            .Select(file => RelativePath(mod, file))
            .Select(file => FileItem.FromFile(mod, file));

        information.Mist.AddRange(files);

        return information;
    }

    public bool CanGenerate(IMod mod) => mod.GetAllFiles(".mist").Count > 0;

    public Validation Validate(IMod mod)
    {
        return new Validation();
    }

    private static string RelativePath(IMod mod, string filePath)
    {
        var basePath = mod.GetBasePath();

        if (Path.IsPathRooted(filePath))
            return Path.GetRelativePath(basePath, filePath).Replace('\\', '/');

        var normalizedBase = basePath.Replace('\\', '/').TrimEnd('/') + '/';
        var normalizedFull = filePath.Replace('\\', '/');
        if (normalizedBase != "/" &&
            normalizedFull.StartsWith(normalizedBase, StringComparison.OrdinalIgnoreCase))
            return normalizedFull[normalizedBase.Length..];
        return normalizedFull;
    }
}