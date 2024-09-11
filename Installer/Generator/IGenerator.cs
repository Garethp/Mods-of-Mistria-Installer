namespace Garethp.ModsOfMistriaInstaller.Installer.Generator;

public interface IGenerator
{
    public GeneratedInformation Generate(string modLocation);
    
    public bool CanGenerate(string modLocation);
}