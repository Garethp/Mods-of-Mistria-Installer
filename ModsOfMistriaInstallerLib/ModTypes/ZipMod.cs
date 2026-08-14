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

    private List<string> _requiredHooks = [];

    private bool _requiredHooksValid = true;

    public ZipMod(ZipArchive zipFile, string basePath) : this()
    {
        _zipFile = zipFile;
        _basePath = NormalizeArchivePath(basePath).Trim('/');

        var manifestFile = FindEntry(ResolvePath("manifest.json")) ?? FindEntry(ResolvePath("manifest.toml"));
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
        _requiredHooks = manifest.RequiresHooks;
        _requiredHooksValid = manifest.RequiresHooksValid;

    }

    private static string NormalizeArchivePath(string path) =>
        ValidateArchivePath(path.Replace('\\', '/'));

    private static string ValidateArchivePath(string path)
    {
        if (path.StartsWith('/') || path.Contains(':'))
            throw new InvalidDataException("Archive path must be relative.");

        var normalized = path.TrimStart('/');
        if (normalized.Split('/').Any(part => part is "." or ".."))
            throw new InvalidDataException("Archive path contains an unsafe segment.");
        return normalized;
    }

    private string ResolvePath(string path)
    {
        var normalized = NormalizeArchivePath(path);
        if (string.IsNullOrEmpty(_basePath)) return normalized;

        var basePath = _basePath + "/";
        return normalized.StartsWith(basePath, StringComparison.OrdinalIgnoreCase)
            ? normalized
            : basePath + normalized;
    }

    private ZipArchiveEntry? FindEntry(string path)
    {
        if (_zipFile is null) return null;

        var normalized = NormalizeArchivePath(path);
        return _zipFile.Entries.FirstOrDefault(entry =>
            !entry.FullName.EndsWith('/') &&
            NormalizeArchivePath(entry.FullName).Equals(normalized, StringComparison.OrdinalIgnoreCase));
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
        _validation.Clear();
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
        
        try
        {
            var currentVersion = InstallerVersion.ModCompatibilityVersion;
            var requiredVersion = new Version(GetMinimumInstallerVersion());
            var newEngineVersion = new Version("0.12");
            
            if (requiredVersion.CompareTo(newEngineVersion) < 0)
            {
                _validation.Errors.Add(new ValidationMessage(this, Path.Combine(GetLocation(), "manifest.json"), Resources.CoreManifestHasNoMinimunInstallerVersion));
            }

            // TODO: Remove the workaround for 1.0.0 after the 12th of July
            if (requiredVersion.CompareTo(currentVersion) > 0 && requiredVersion.CompareTo(new Version("1.0")) < 0)
            {
                _validation.Errors.Add(new ValidationMessage(this, Path.Combine(GetLocation(), "manifest.json"), Resources.CoreModRequiresNewerInstaller));
            }
        }
        catch (Exception)
        {
            _validation.Errors.Add(new ValidationMessage(this, Path.Combine(GetLocation(), "manifest.json"), string.Format(Resources.CoreErrorReadingVersionForMod, GetId())));
        }
        
        if (new Version(_minimumInstallerVersion).CompareTo(new Version("1.0")) > -1)
        {
            _validation.Warnings.Add(new ValidationMessage(this, Path.Combine(GetLocation(), "manifest.json"), Resources.CoreModRequiresIncorrectVersion));
        }

        FolderMod.ValidateGmlManifestFields(_validation, this, Path.Combine(GetLocation(), "manifest.json"),
            _requiredHooksValid);

        return _validation;
    }

    public bool HasFilesInFolder(string folder) => HasFilesInFolder(folder, "");

    public bool HasFilesInFolder(string folder, string extension) =>
        GetFilesInFolder(folder, extension).Count > 0;

    public bool FileExists(string path) => FindEntry(ResolvePath(path)) is not null;

    public bool FolderExists(string path)
    {
        if (_zipFile is null) return false;

        var prefix = ResolvePath(path).TrimEnd('/') + "/";
        return _zipFile.Entries.Any(entry =>
            NormalizeArchivePath(entry.FullName).StartsWith(prefix, StringComparison.OrdinalIgnoreCase));
    }

    public List<string> GetFilesInFolder(string folder) => GetFilesInFolder(folder, "");

    public List<string> GetAllFiles(string extension)
    {
        if (_zipFile is null) return [];

        return _zipFile.Entries
            .Where(entry => !entry.FullName.EndsWith('/') &&
                            NormalizeArchivePath(entry.FullName).EndsWith(extension, StringComparison.OrdinalIgnoreCase))
            .Select(entry => NormalizeArchivePath(entry.FullName))
            .ToList();
    }

    public List<string> GetFilesInFolder(string folder, string? extension)
    {
        if (_zipFile is null) return [];

        var prefix = ResolvePath(folder).TrimEnd('/') + "/";
        return _zipFile.Entries
            .Where(entry => !entry.FullName.EndsWith('/') &&
                            NormalizeArchivePath(entry.FullName).StartsWith(prefix, StringComparison.OrdinalIgnoreCase) &&
                            NormalizeArchivePath(entry.FullName).EndsWith(extension ?? "", StringComparison.OrdinalIgnoreCase))
            .Select(entry => NormalizeArchivePath(entry.FullName))
            .ToList();
    }

    public string ReadFile(string path)
    {
        var entry = FindEntry(ResolvePath(path));
        return entry is null ? "" : readEntry(entry);
    }

    public Stream ReadFileAsStream(string path)
    {
        if (_zipFile is null) throw new Exception("Cannot read file from zip file");
        var entry = FindEntry(ResolvePath(path));
        if (entry is null) throw new Exception("Cannot read file from zip file");

        return entry.Open();
    }

    public List<ModRequirement> GetRequirements() => _requirements;

    public List<string> GetRequiredHooks() => _requiredHooks;

    public string? GetUpdateUrl()   => _updateUrl;
    public string? GetDownloadUrl() => _downloadUrl;
}
