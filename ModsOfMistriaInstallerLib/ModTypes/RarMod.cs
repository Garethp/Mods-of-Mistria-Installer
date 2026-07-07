using System.Reflection;
using System.Text.RegularExpressions;
using Garethp.ModsOfMistriaInstallerLib.Generator;
using Garethp.ModsOfMistriaInstallerLib.Lang;
using Newtonsoft.Json.Linq;
using SharpCompress.Archives.Rar;
using Tomlyn;
using Tomlyn.Model;

namespace Garethp.ModsOfMistriaInstallerLib.ModTypes;

public class RarMod() : IMod
{
    private string _name = "";

    private string _author = "";

    private string _version = "";

    private string _minimumInstallerVersion = "0.1.0";

    private string _manifestVersion = "1";

    private Validation _validation = new Validation();

    private RarArchive? _rarFile;

    private string _basePath = "";
    
    private bool _isInstalled = false;

    private List<ModRequirement> _requirements = [];

    private string? _updateUrl;

    private string? _downloadUrl;

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
        var manifestFile = GetEntry(rarFile, basePath + "manifest.json") ?? GetEntry(rarFile, basePath + "manifest.toml");
        if (manifestFile is null) return;

        ModManifest manifest;
        if (manifestFile.Key.EndsWith(".json"))
        {
            manifest = ModManifest.FromJson(JObject.Parse(ReadEntry(manifestFile)));
        } else if (manifestFile.Key.EndsWith(".toml"))
        {
            manifest = ModManifest.FromToml(TomlSerializer.Deserialize<TomlTable>(ReadEntry(manifestFile))!);
        }
        else return;

        _name = manifest.Name;
        _author = manifest.Author;
        _version = manifest.Version;
        _minimumInstallerVersion = manifest.MinInstallerVersion;
        _manifestVersion = manifest.ManifestVersion;
        _requirements = manifest.Requirements;
        _downloadUrl = manifest.DownloadUrl;
        _updateUrl = manifest.UpdateUrl;
        
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
        var entryStream = entry.OpenEntryStream();
        using var reader = new StreamReader(entryStream);
        var contents = reader.ReadToEnd();

        return contents;
    }

    public static RarMod? FromRarFile(string rarPath)
    {
        if (!File.Exists(rarPath)) return null;

        var rarFile = RarArchive.Open(rarPath);

        var manifestFiles = rarFile.Entries.Where(entry => entry.Key.EndsWith("manifest.json") || entry.Key.EndsWith("manifest.toml")).ToList();

        if (manifestFiles.Count != 1) return null;

        var internalLocation = manifestFiles.First().Key.Replace("manifest.json", "").Replace("manifest.toml", "");

        return new RarMod(rarFile, internalLocation);
    }

    public string GetAuthor() => _author;

    public string GetName() => _name;

    public string GetVersion() => _version;

    public string GetLocation() => "";

    public string GetMinimumInstallerVersion() => _minimumInstallerVersion;

    public string GetManifestVersion() => _manifestVersion;

    public Validation GetValidation() => _validation;

    public string GetBasePath() => _basePath;
    
    public bool IsInstalled() => _isInstalled;
    
    public void SetInstalled(bool installed) => _isInstalled = installed;

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
                Resources.CoreManifestHasNoAuthor));
        }

        if (string.IsNullOrEmpty(GetName()))
        {
            _validation.Errors.Add(new ValidationMessage(this, Path.Combine(GetLocation(), "manifest.json"),
                Resources.CoreManifestHasNoName));
        }

        if (string.IsNullOrEmpty(GetVersion()))
        {
            _validation.Errors.Add(new ValidationMessage(this, Path.Combine(GetLocation(), "manifest.json"),
                Resources.CoreManifestHasNoVersion));
        }
        
        var canInstall = CanInstall();
        if (!string.IsNullOrEmpty(canInstall))
        {
            _validation.Errors.Add(new ValidationMessage(this, Path.Combine(GetLocation(), "manifest.json"), canInstall));
        }

        return _validation;
    }

    public string? CanInstall()
    {
        try
        {
            var currentExe = Assembly.GetEntryAssembly();
            var currentVersionString =
                currentExe!.GetCustomAttribute<AssemblyFileVersionAttribute>()?.Version ?? "0.1.0";
            var currentVersion = new Version(currentVersionString);
            var requiredVersion = GetMinimumInstallerVersion();
            var newEngineVersion = new Version("0.12.0");
            
            if (requiredVersion.CompareTo(newEngineVersion) < 0)
            {
                return Resources.CoreManifestHasNoMinimunInstallerVersion;
            }

            if (requiredVersion.CompareTo(currentVersion) > 0)
            {
                return Resources.CoreModRequiresNewerInstaller;
            }
        }
        catch (Exception)
        {
            return string.Format(Resources.CoreErrorReadingVersionForMod, GetId());
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
        if (_rarFile is null) return [];

        return _rarFile.Entries
            .Where(entry => !entry.IsDirectory && entry.Key.EndsWith(extension))
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

    public List<ModRequirement> GetRequirements() => _requirements;

    public string? GetUpdateUrl()   => _updateUrl;
    public string? GetDownloadUrl() => _downloadUrl;
}