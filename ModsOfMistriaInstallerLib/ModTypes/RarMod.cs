using System.Reflection;
using System.Text.RegularExpressions;
using Garethp.ModsOfMistriaInstallerLib.Generator;
using Garethp.ModsOfMistriaInstallerLib.Lang;
using Newtonsoft.Json.Linq;
using SharpCompress.Archives.Rar;

namespace Garethp.ModsOfMistriaInstallerLib.ModTypes;

public class RarMod() : IMod
{
    private string _name = "";

    private string _author = "";

    private string _version = "";

    private string _minimunInstallerVersion = "0.1.0";

    private string _manifestVersion = "1";

    private Validation _validation = new Validation();

    private RarArchive? _rarFile;

    private string _basePath = "";
    
    private RarArchiveEntry? GetEntry(RarArchive rarFile, string path)
    {
        var isDirectory = path.EndsWith('/');
        if (isDirectory)
        {
            path = path[..^1];
        }

        path = path.Replace('/', Path.DirectorySeparatorChar);
        
        return rarFile.Entries.FirstOrDefault(entry => entry.Key == path && entry.IsDirectory == isDirectory);
    }
    
    private RarArchiveEntry? GetEntry(string path) => GetEntry(_rarFile, path);

    private RarMod(RarArchive rarFile, string basePath) : this()
    {
        var manifestFile = GetEntry(rarFile, basePath + "manifest.json");
        if (manifestFile is null) return;

        var manifest = JObject.Parse(ReadEntry(manifestFile));

        _name = manifest["name"]?.ToString() ?? "";
        _author = manifest["author"]?.ToString() ?? "";
        _version = manifest["version"]?.ToString() ?? "1.0.0";
        _minimunInstallerVersion = manifest["minInstallerVersion"]?.ToString() ?? "0.1.0";
        _manifestVersion = manifest["manifestVersion"]?.ToString() ?? "1";
        _rarFile = rarFile;
        _basePath = basePath;
    }

    private string ReadEntry(RarArchive? rarFile, string entryName)
    {
        if (rarFile is null) return "";
        var entry = GetEntry(entryName);
        return entry is null ? "" : ReadEntry(entry);
    }

    private string ReadEntry(RarArchiveEntry entry)
    {
        Stream entryStream = entry.OpenEntryStream();
        using var reader = new StreamReader(entryStream);
        var contents = reader.ReadToEnd();

        return contents;
    }

    public static RarMod FromRarFile(string rarPath)
    {
        var rarMod = new RarMod();

        if (!File.Exists(rarPath)) return rarMod;

        var rarFile = RarArchive.Open(rarPath);

        var manifestFiles = rarFile.Entries.Where(entry => entry.Key.EndsWith("manifest.json"));

        if (manifestFiles.Count() != 1) return rarMod;

        var internalLocation = manifestFiles.First().Key.Replace("manifest.json", "");

        return new RarMod(rarFile, internalLocation);
    }

    public string GetAuthor() => _author;

    public string GetName() => _name;

    public string GetVersion() => _version;

    public string GetLocation() => "";

    public string GetMinimunInstallerVersion() => _minimunInstallerVersion;

    public string GetManifestVersion() => _manifestVersion;

    public Validation GetValidation() => _validation;

    public string GetBasePath() => _basePath;

    public string GetId()
    {
        var initialId = $"{GetAuthor().ToLower()}.{GetName().ToLower()}".Replace(" ", "_");
        return Regex.Replace(initialId, "[^a-zA-Z0-9_\\.]", "");
    }

    public Validation Validate()
    {
        if (string.IsNullOrEmpty(GetAuthor()))
        {
            _validation.Errors.Add(new ValidationMessage(this, Path.Combine(GetLocation(), "manifest.json"),
                Resources.ManifestHasNoAuthor));
        }

        if (string.IsNullOrEmpty(GetName()))
        {
            _validation.Errors.Add(new ValidationMessage(this, Path.Combine(GetLocation(), "manifest.json"),
                Resources.ManifestHasNoName));
        }

        if (string.IsNullOrEmpty(GetVersion()))
        {
            _validation.Errors.Add(new ValidationMessage(this, Path.Combine(GetLocation(), "manifest.json"),
                Resources.ManifestHasNoVersion));
        }

        return _validation;
    }

    public string? CanInstall()
    {
        var currentExe = Assembly.GetEntryAssembly();
        var currentVersionString =
            currentExe!.GetCustomAttribute<AssemblyFileVersionAttribute>()?.Version ?? "0.1.0";
        var currentVersion = new Version(currentVersionString);
        var requiredVersion = new Version(GetMinimunInstallerVersion());

        if (requiredVersion.CompareTo(currentVersion) > 0)
        {
            return Resources.ModRequiresNewerInstaller;
        }

        return null;
    }

    public bool HasFilesInFolder(string folder) => HasFilesInFolder(folder, "");

    public bool HasFilesInFolder(string folder, string extension) => _rarFile is not null && _rarFile.Entries.Any(
        entry =>
            entry.Key.StartsWith($"{_basePath}{folder}".Replace('/', Path.DirectorySeparatorChar)) &&
            !entry.IsDirectory);

    public bool FileExists(string path) => _rarFile is not null &&
                                           _rarFile.Entries.Any(entry =>
                                               entry.Key == $"{_basePath}{path}".Replace('/', Path.DirectorySeparatorChar) && !entry.IsDirectory);

    public bool FolderExists(string path) => GetEntry($"{_basePath}{path}/") != null;

    public List<string> GetFilesInFolder(string folder) => GetFilesInFolder(folder, "");

    public List<string> GetAllFiles(string extension)
    {
        if (_rarFile is null) return new List<string>();

        return _rarFile.Entries
            .Where(entry => !entry.IsDirectory && entry.Key.EndsWith(extension ?? ""))
            .Select(entry => entry.Key)
            .ToList();
    }

    public List<string> GetFilesInFolder(string folder, string? extension) =>
        _rarFile?.Entries
            .Where(entry => entry.Key.StartsWith($"{_basePath}{folder}".Replace('/', Path.DirectorySeparatorChar)) && !entry.IsDirectory &&
                            entry.Key.EndsWith(extension ?? ""))
            .Select(entry => entry.Key).ToList() ?? new List<string>();

    public string ReadFile(string path)
    {
        if (!path.StartsWith(_basePath)) path = $"{_basePath}{path}";

        return ReadEntry(_rarFile, path.Replace('/', Path.DirectorySeparatorChar));
    }

    public Stream ReadFileAsStream(string path)
    {
        if (!path.StartsWith(_basePath)) path = $"{_basePath}{path}";

        if (_rarFile is null) throw new Exception("Cannot read file from rar file");
        var entry = GetEntry($"{path}");
        if (entry is null) throw new Exception("Cannot read file from rar file");

        return entry.OpenEntryStream();
    }
}