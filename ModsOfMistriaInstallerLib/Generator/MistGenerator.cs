namespace Garethp.ModsOfMistriaInstallerLib.Generator;

[InformationGenerator(1)]
public class MistGenerator: IGenerator
{
    public GeneratedInformation Generate(Mod mod)
    {
        throw new NotImplementedException();
    }

    public bool CanGenerate(Mod mod) => false;
    
    public Validation Validate(Mod mod) => new Validation();
}