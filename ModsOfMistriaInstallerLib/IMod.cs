using Garethp.ModsOfMistriaInstallerLib.Generator;

namespace Garethp.ModsOfMistriaInstallerLib;

public interface IMod
{
    public string GetAuthor();

    public string GetName();

    public string GetVersion();

    public string GetLocation();

    public string GetMinimunInstallerVersion();

    public string GetManifestVersion();
    
    public Validation GetValidation();

    public string GetId();

    public Validation Validate();

    public string? CanInstall();
    
    public bool HasFilesInFolder(string folder);

    public List<string> GetFilesInFolder(string folder);
    
    public bool FileExists(string path);
    
    public bool FolderExists(string path);
    
    public string ReadFile(string path);
    
    public Stream ReadFileAsStream(string path);
}