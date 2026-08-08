using System.Text;
using Garethp.ModsOfMistriaInstallerLib.Generator;
using Garethp.ModsOfMistriaInstallerLib.ModTypes;

namespace ModsOfMistriaInstallerLibTests.Fixtures;

public class MockMod : IMod
{
    private readonly Dictionary<string, Dictionary<string, object>> _files = new();

    private readonly Validation _validation = new();

    public string Id { get; init; } = "mock.mod";

    public string Name { get; init; } = "Mock Mod";

    public string Author { get; init; } = "mock";

    public string Version { get; init; } = "1.0";

    // Stands in for the mod's folder name, which may differ from its id
    public string DirName { get; init; } = "mock_mod";

    public List<string> RequiredHooks { get; init; } = [];

    public MockMod(List<string> files)
    {
        files.ForEach(file =>
        {
            if (Path.HasExtension(file))
            {
                var folderName = Path.GetDirectoryName(file)?.Replace('\\', '/') ?? "";

                if (!_files.ContainsKey(folderName)) _files[folderName] = new();
                _files[folderName][file] = "";
            }
            else
            {
                if (!_files.ContainsKey(file)) _files[file] = new();
            }
        });
    }

    public MockMod(Dictionary<string, object> files)
    {
        files.Keys.ToList().ForEach(file => { SetFile(file, files[file]); });
    }

    public void SetFile(string file, object contents)
    {
        if (Path.HasExtension(file))
        {
            var folderName = Path.GetDirectoryName(file)?.Replace('\\', '/') ?? "";

            if (!_files.ContainsKey(folderName)) _files[folderName] = new();
            if (contents is not string and not byte[])
                throw new NotImplementedException();

            _files[folderName][file] = contents;
        }
        else
        {
            if (!_files.ContainsKey(file)) _files[file] = new();
        }
    }

    public void RemoveFile(string file)
    {
        if (!Path.HasExtension(file)) return;
        var folderName = Path.GetDirectoryName(file)?.Replace('\\', '/') ?? "";

        if (!_files.ContainsKey(folderName)) return;
        _files[folderName].Remove(file);
    }

    public string GetAuthor() => Author;

    public string GetName() => Name;

    public string GetVersion() => Version;

    public string GetLocation() => DirName;

    public string GetMinimumInstallerVersion() => "1.0";

    public string GetManifestVersion() => "1";

    public Validation GetValidation() => _validation;

    public string GetId() => Id;

    public Validation Validate() => _validation;

    public string GetBasePath() => "";

    public bool IsInstalled() => false;

    public void SetInstalled(bool installed)
    {
    }

    public bool HasFilesInFolder(string folder, string extension) =>
        _files.TryGetValue(folder, out var files) && files.Keys.Any(f => f.EndsWith(extension));

    public bool HasFilesInFolder(string folder) => HasFilesInFolder(folder, "");

    public List<string> GetFilesInFolder(string folder, string extension) =>
        GetFilesInFolder(folder).Where(f => f.EndsWith(extension)).ToList();

    public List<string> GetFilesInFolder(string folder)
    {
        if (!_files.ContainsKey(folder)) return [];

        return _files[folder].Keys.ToList();
    }

    public List<string> GetAllFiles(string extension) => _files.Values
        .SelectMany(files => files.Keys)
        .Where(file => file.EndsWith(extension))
        .ToList();

    public bool FileExists(string path) => _files.Values.Any(files => files.Keys.Contains(path));

    public bool FolderExists(string path) => _files.Keys.Contains(path);

    private object ReadFileInternal(string path)
    {
        foreach (var folder in _files.Keys)
        {
            foreach (var file in _files[folder])
            {
                if (file.Key == path)
                {
                    return file.Value;
                }
            }
        }

        throw new FileNotFoundException(path);
    }

    public string ReadFile(string path)
    {
        return ReadFileInternal(path) switch
        {
            string stringValue => stringValue,
            byte[] byteValue => Encoding.Default.GetString(byteValue),
            _ => throw new NotImplementedException()
        };
    }

    public Stream ReadFileAsStream(string path)
    {
        return ReadFileInternal(path) switch
        {
            string stringValue => new MemoryStream(Encoding.UTF8.GetBytes(ReadFile(path))),
            byte[] byteValue => new MemoryStream(byteValue),
            _ => throw new NotImplementedException()
        };
    }

    public List<ModRequirement> GetRequirements() => [];

    public List<string> GetRequiredHooks() => RequiredHooks;

    public string? GetUpdateUrl()   => null;
    public string? GetDownloadUrl() => null;
}
