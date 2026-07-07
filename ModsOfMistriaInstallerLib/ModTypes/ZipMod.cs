using System.IO.Compression;
using System.Reflection;
using System.Text.RegularExpressions;
using Garethp.ModsOfMistriaInstallerLib.Generator;
using Garethp.ModsOfMistriaInstallerLib.Lang;
using Newtonsoft.Json.Linq;
using Tomlyn;
using Tomlyn.Model;

namespace Garethp.ModsOfMistriaInstallerLib.ModTypes;

public class ZipMod() : IMod
{
    private string _name = "";

    private string _author = "";

    private string _version = "";

    private string _minimumInstallerVersion = "";

    private string _manifestVersion = "";

    private Validation _validation = new Validation();

    private ZipArchive? _zipFile;

    private string _basePath = "";
    
    private bool _isInstalled = false;

    private List<ModRequirement> _requirements = [];

    private string? _updateUrl;

    private string? _downloadUrl;

    public ZipMod(ZipArchive zipFile, string basePath) : this()
    {
        var manifestFile = zipFile.GetEntry(basePath + "manifest.json") ?? zipFile.GetEntry(basePath + "manifest.toml");
        if (manifestFile is null) return;

        ModManifest manifest;
        if (manifestFile.Name.EndsWith(".json"))
        {
            manifest = ModManifest.FromJson(JObject.Parse(readEntry(manifestFile)));
        } else if (manifestFile.Name.EndsWith(".toml"))
        {
            manifest = ModManifest.FromToml(TomlSerializer.Deserialize<TomlTable>(readEntry(manifestFile))!);
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
        
        _zipFile = zipFile;
        _basePath = basePath;
    }

    private string readEntry(ZipArchive? zipFile, string entryName)
    {
        if (zipFile is null) return "";
        var entry = zipFile.GetEntry(entryName);
        return entry is null ? "" : readEntry(entry);
    }

    private string readEntry(ZipArchiveEntry entry)
    {
        var entryStream = entry.Open();
        using var reader = new StreamReader(entryStream);
        var contents = reader.ReadToEnd();

        return contents;
    }

    public static ZipMod? FromZipFile(string ZipPath)
    {
        if (!File.Exists(ZipPath)) return null;

        var zipFile = ZipFile.OpenRead(ZipPath);

        var manifestFiles = zipFile.Entries.Where(entry => entry.Name is "manifest.json" or "manifest.toml").ToList();

        if (manifestFiles.Count() != 1) return null;

        var internalLocation = manifestFiles.First().FullName.Replace("manifest.json", "").Replace("manifest.toml", "");

        return new ZipMod(zipFile, internalLocation);
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

        if (new Version(_minimumInstallerVersion).Equals(new Version("1.0.0")))
        {
            _validation.Warnings.Add(new ValidationMessage(this, Path.Combine(GetLocation(), "manifest.json"), Resources.CoreModRequiresIncorrectVersion));
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
            var requiredVersion = new Version(GetMinimumInstallerVersion());
            var newEngineVersion = new Version("0.12.0");
            
            if (requiredVersion.CompareTo(newEngineVersion) < 0)
            {
                return Resources.CoreManifestHasNoMinimunInstallerVersion;
            }

            // TODO: Remove the workaround for 1.0.0 after the 12th of July
            if (requiredVersion.CompareTo(currentVersion) > 0 && !requiredVersion.Equals(new Version("1.0.0")))
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

    public bool HasFilesInFolder(string folder, string extension) => _zipFile is not null && _zipFile.Entries.Any(
        entry =>
            entry.FullName.StartsWith($"{_basePath}{folder}") && !entry.FullName.EndsWith('/') &&
            entry.FullName.EndsWith(extension));

    public bool FileExists(string path) => _zipFile is not null &&
                                           _zipFile.Entries.Any(entry =>
                                               entry.FullName == $"{_basePath}{path}" && !entry.FullName.EndsWith('/'));

    public bool FolderExists(string path) => _zipFile?.GetEntry($"{_basePath}{path}/") != null;

    public List<string> GetFilesInFolder(string folder) => GetFilesInFolder(folder, "");

    public List<string> GetAllFiles(string extension)
    {
        if (_zipFile is null) return [];

        return _zipFile.Entries
            .Where(entry => !entry.FullName.EndsWith('/') && entry.FullName.EndsWith(extension))
            .Select(entry => entry.FullName)
            .ToList();
    }

    public List<string> GetFilesInFolder(string folder, string? extension) =>
        _zipFile?.Entries
            .Where(entry => entry.FullName.StartsWith($"{_basePath}{folder}") && !entry.FullName.EndsWith('/') &&
                            entry.FullName.EndsWith(extension ?? ""))
            .Select(entry => entry.FullName).ToList() ?? [];

    public string ReadFile(string path)
    {
        if (!path.StartsWith(_basePath)) path = $"{_basePath}{path}";

        return readEntry(_zipFile, path);
    }

    public Stream ReadFileAsStream(string path)
    {
        if (!path.StartsWith(_basePath)) path = $"{_basePath}{path}";

        if (_zipFile is null) throw new Exception("Cannot read file from zip file");
        var entry = _zipFile.GetEntry($"{path}");
        if (entry is null) throw new Exception("Cannot read file from zip file");

        return entry.Open();
    }

    public List<ModRequirement> GetRequirements() => _requirements;

    public string? GetUpdateUrl()   => _updateUrl;
    public string? GetDownloadUrl() => _downloadUrl;
}