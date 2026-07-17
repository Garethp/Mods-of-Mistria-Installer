using System.Text;
using Garethp.ModsOfMistriaInstallerLib.Generator;
using Garethp.ModsOfMistriaInstallerLib.ModTypes;

namespace ModsOfMistriaInstallerLibTests.Fixtures;

public class MockMod : IMod
{
    private readonly Dictionary<string, Dictionary<string, string>> _files = new();

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

    public MockMod(Dictionary<string, string> files)
    {
        files.Keys.ToList().ForEach(file =>
        {
            if (Path.HasExtension(file))
            {
                var folderName = Path.GetDirectoryName(file)?.Replace('\\', '/') ?? "";

                if (!_files.ContainsKey(folderName)) _files[folderName] = new();
                _files[folderName][file] = files[file];
            }
            else
            {
                if (!_files.ContainsKey(file)) _files[file] = new();
            }
        });
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

    public bool HasFilesInFolder(string folder) => _files.ContainsKey(folder) && _files[folder].Count > 0;

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

    public string ReadFile(string path)
    {
        foreach (var folder in _files.Keys)
        {
            foreach (var file in _files[folder])
            {
                if (file.Key == path) return file.Value;
            }
        }

        throw new FileNotFoundException(path);
    }

    public Stream ReadFileAsStream(string path) => new MemoryStream(Encoding.UTF8.GetBytes(ReadFile(path)));

    public List<ModRequirement> GetRequirements() => [];

    public List<string> GetRequiredHooks() => RequiredHooks;

    public string? GetUpdateUrl()   => null;
    public string? GetDownloadUrl() => null;
}
