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
            .Select(file => FileItem.FromFile(mod, file));
        
        information.Mist.AddRange(files);
        
        return information;
    }

    public bool CanGenerate(IMod mod) => mod.GetAllFiles(".mist").Count > 0;

    public Validation Validate(IMod mod)
    {
        return new Validation();
    }
}