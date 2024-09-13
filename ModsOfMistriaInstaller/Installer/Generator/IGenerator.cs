namespace Garethp.ModsOfMistriaInstaller.Installer.Generator;

public interface IGenerator
{
    public GeneratedInformation Generate(Mod mod);
    
    public bool CanGenerate(Mod mod);
}