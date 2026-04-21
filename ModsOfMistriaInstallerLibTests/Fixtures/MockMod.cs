using Garethp.ModsOfMistriaInstallerLib.Generator;
using Garethp.ModsOfMistriaInstallerLib.ModTypes;

namespace ModsOfMistriaInstallerLibTests.Fixtures;

public class MockMod : IMod
{
    private readonly Dictionary<string, Dictionary<string, string>> _files = new();
    
    private readonly Validation _validation = new();
    private readonly string _name = "mod";

    public MockMod(List<string> files)
    {
        files.ForEach(file =>
        {
            if (Path.HasExtension(file))
            {
                var folderName = Path.GetDirectoryName(file)?.Replace('\\', '/');
                
                if (!_files.ContainsKey(folderName)) _files[folderName] = new();
                _files[folderName][file] = "";
            }
            else
            {
                if (!_files.ContainsKey(file)) _files[file] = new();
            }
        });
    }

    public MockMod(Dictionary<string, string> files) : this("mod", files) { }

    public MockMod(string modName, Dictionary<string, string> files)
    {
        _name = modName;
        
        files.Keys.ToList().ForEach(file =>
        {
            if (Path.HasExtension(file))
            {
                var folderName = Path.GetDirectoryName(file)?.Replace('\\', '/');
                
                if (!_files.ContainsKey(folderName)) _files[folderName] = new();
                _files[folderName][file] = files[file];
            }
            else
            {
                if (!_files.ContainsKey(file)) _files[file] = new();
            }
        });
    }

    public string GetAuthor()
    {
        throw new NotImplementedException();
    }

    public string GetName() => _name;

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

    public string GetManifestVersion()
    {
        throw new NotImplementedException();
    }

    public Validation GetValidation()
    {
        return _validation;
    }

    public string GetId() => "mock.mod";

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
        if (!_files.ContainsKey(folder)) return [];

        return _files[folder].Keys.ToList();
    }

    public List<string> GetAllFiles(string extension)
    {
        throw new NotImplementedException();
    }

    public bool FileExists(string path) => _files.Values.Any(files => files.Keys.Contains(path));

    public bool FolderExists(string path) => _files.Keys.Contains(path);

    public string ReadFile(string path)
    {
        foreach (var folder in _files.Keys)
        {
            foreach (var file in _files[folder])
            {
                if (file.Key == path) return file.Value;
            }
        }

        throw new NotImplementedException();
    }

    public Stream ReadFileAsStream(string path)
    {
        throw new NotImplementedException();
    }

    }