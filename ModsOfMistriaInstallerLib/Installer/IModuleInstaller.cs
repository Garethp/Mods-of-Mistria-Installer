namespace Garethp.ModsOfMistriaInstallerLib.Installer;

public interface IModuleInstaller
{
    public void Install(string fieldsOfMistriaLocation, GeneratedInformation information, Action<string, string> reportStatus);
}

[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public class InformationInstaller(int manifestVersion) : Attribute
{
    public int ManifestVersion { get; } = manifestVersion;
}