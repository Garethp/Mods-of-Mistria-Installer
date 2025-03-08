using Garethp.ModsOfMistriaInstallerLib.Generator;

namespace Garethp.ModsOfMistriaInstallerLib.ModTypes;

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
    
    public bool IsInstalled();

    public void SetInstalled(bool installed);

    public Validation Validate();

    public string GetBasePath();

    public string? CanInstall();
    
    public bool HasFilesInFolder(string folder, string extension);
    
    public bool HasFilesInFolder(string folder);
    
    public List<string> GetFilesInFolder(string folder, string extension);
    
    public List<string> GetFilesInFolder(string folder);
    
    public List<string> GetAllFiles(string extension);
    
    public bool FileExists(string path);
    
    public bool FolderExists(string path);
    
    public string ReadFile(string path);
    
    public Stream ReadFileAsStream(string path);
}