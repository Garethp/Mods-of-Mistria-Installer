using Garethp.ModsOfMistriaInstallerLib.Utils;

namespace Garethp.ModsOfMistriaInstallerLib.Installer;

public interface IModuleInstaller
{
    public void Install(string fieldsOfMistriaLocation, string modsLocation, GeneratedInformation information, Action<string, string> reportStatus);
    
    public void SetFileModifier(IFileModifier fileModifier);
}

[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public class InformationInstaller(int manifestVersion) : Attribute
{
    public int ManifestVersion { get; } = manifestVersion;
}