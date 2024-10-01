namespace Garethp.ModsOfMistriaInstallerLib;

public interface IPreinstallInfo
{
    public List<string> GetPreinstallInformation(GeneratedInformation information);
}

public interface IPreUninstallInfo
{
    public List<string> GetPreUninstallInformation();
}