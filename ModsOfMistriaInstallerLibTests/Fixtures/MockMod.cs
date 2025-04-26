using Garethp.ModsOfMistriaInstallerLib.Generator;
using Garethp.ModsOfMistriaInstallerLib.ModTypes;

namespace ModsOfMistriaInstallerLibTests.Fixtures;

public class MockMod : IMod
{
    private readonly Dictionary<string, List<string>> _files = new();

    public MockMod(List<string> files)
    {
        files.ForEach(file =>
        {
            if (Path.HasExtension(file))
            {
                var folderName = Path.GetDirectoryName(file)?.Replace('\\', '/');
                
                if (!_files.ContainsKey(folderName)) _files.Add(folderName, []);
                _files[folderName].Add(file);
            }
            else
            {
                if (!_files.ContainsKey(file)) _files.Add(file, []);
            }
        });
    }

    public string GetAuthor()
    {
        throw new NotImplementedException();
    }

    public string GetName()
    {
        throw new NotImplementedException();
    }

    public string GetVersion()
    {
        throw new NotImplementedException();
    }

    public string GetLocation()
    {
        throw new NotImplementedException();
    }

    public string GetMinimunInstallerVersion()
    {
        throw new NotImplementedException();
    }
    
    public string GetMinimumAurieVersion()
    {
        throw new NotImplementedException();
    }

    public string GetManifestVersion()
    {
        throw new NotImplementedException();
    }

    public Validation GetValidation()
    {
        throw new NotImplementedException();
    }

    public string GetId()
    {
        throw new NotImplementedException();
    }

    public Validation Validate()
    {
        throw new NotImplementedException();
    }

    public string GetBasePath()
    {
        throw new NotImplementedException();
    }

    public string? CanInstall()
    {
        throw new NotImplementedException();
    }

    public bool IsInstalled()
    {
        throw new NotImplementedException();
    }
    
    public void SetInstalled(bool installed)
    {
        throw new NotImplementedException();
    }

    public bool HasFilesInFolder(string folder, string extension)
    {
        throw new NotImplementedException();
    }

    public bool HasFilesInFolder(string folder) => _files.ContainsKey(folder) && _files[folder].Count > 0;

    public List<string> GetFilesInFolder(string folder, string extension)
    {
        throw new NotImplementedException();
    }

    public List<string> GetFilesInFolder(string folder)
    {
        throw new NotImplementedException();
    }

    public List<string> GetAllFiles(string extension)
    {
        throw new NotImplementedException();
    }

    public bool FileExists(string path) => _files.Values.Any(files => files.Contains(path));

    public bool FolderExists(string path) => _files.Keys.Contains(path);

    public string ReadFile(string path)
    {
        throw new NotImplementedException();
    }

    public Stream ReadFileAsStream(string path)
    {
        throw new NotImplementedException();
    }
}