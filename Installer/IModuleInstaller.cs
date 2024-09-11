namespace Garethp.ModsOfMistriaInstaller.Installer;

public interface IModuleInstaller
{
    public void Install(string fieldsOfMistriaLocation, GeneratedInformation information);
}