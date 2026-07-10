using System.Reflection;
using System.Text.RegularExpressions;
using Garethp.ModsOfMistriaInstallerLib.Generator;
using Garethp.ModsOfMistriaInstallerLib.Lang;
using Newtonsoft.Json.Linq;
using Tomlyn;
using Tomlyn.Model;

namespace Garethp.ModsOfMistriaInstallerLib.ModTypes;

public class FolderMod : IMod
{
    private string _author;

    private string _name;

    private string _version;

    private string _location;

    private string _minimumInstallerVersion;

    private string _manifestVersion;

    private Validation _validation = new();

    private bool _isInstalled = false;

    private List<ModRequirement> _requirements = [];

    private string? _updateUrl;

    private string? _downloadUrl;

    public string Id
    {
        get
        {
            var initialId = $"{_author.ToLower()}.{_name.ToLower()}".Replace(" ", "_");
            return Regex.Replace(initialId, "[^a-zA-Z0-9_\\.]", "");
        }
    }

    public string GetAuthor() => _author;

    public string GetName() => _name;

    public string GetVersion() => _version;

    public string GetLocation() => _location;

    public string GetMinimumInstallerVersion() => _minimumInstallerVersion;

    public string GetManifestVersion() => _manifestVersion;

    public Validation GetValidation() => _validation;
    
    public bool IsInstalled() => _isInstalled;
    
    public void SetInstalled(bool installed) => _isInstalled = installed;

    public string GetBasePath() => _location;

    public string GetId() => Id;

    public List<ModRequirement> GetRequirements() => _requirements;

    public string? GetUpdateUrl()   => _updateUrl;
    public string? GetDownloadUrl() => _downloadUrl;

    public static FolderMod FromManifest(string manifestLocation)
    {
        var mod = TryFromManifest(manifestLocation, out var failureReason);
        if (mod is not null) return mod;

        throw new Exception(failureReason ?? Resources.CoreManifestFileNamedIncorrectly);
    }

    /// <summary>
    /// Loads a folder mod when the manifest is readable. Returns null (with reason) instead of
    /// crashing the whole install when a manifest is missing, empty, or malformed.
    /// </summary>
    public static FolderMod? TryFromManifest(string manifestLocation, out string? failureReason)
    {
        failureReason = null;

        if (File.Exists(Path.Combine(manifestLocation, "manifest.json")))
        {
            manifestLocation = Path.Combine(manifestLocation, "manifest.json");
        }
        else if (File.Exists(Path.Combine(manifestLocation, "manifest.toml")))
        {
            manifestLocation = Path.Combine(manifestLocation, "manifest.toml");
        }
        else
        {
            failureReason = Resources.CoreManifestFileNamedIncorrectly;
            return null;
        }

        string contents;
        try
        {
            contents = File.ReadAllText(manifestLocation);
        }
        catch (Exception ex)
        {
            failureReason = $"Could not read manifest: {ex.Message}";
            return null;
        }

        if (string.IsNullOrWhiteSpace(contents))
        {
            failureReason = $"Manifest is empty: {manifestLocation}";
            return null;
        }

        ModManifest manifest;
        try
        {
            if (manifestLocation.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
            {
                manifest = ModManifest.FromJson(JObject.Parse(contents));
            }
            else if (manifestLocation.EndsWith(".toml", StringComparison.OrdinalIgnoreCase))
            {
                var table = TomlSerializer.Deserialize<TomlTable>(contents);
                if (table is null)
                {
                    failureReason = $"Manifest TOML is empty or invalid: {manifestLocation}";
                    return null;
                }

                manifest = ModManifest.FromToml(table);
            }
            else
            {
                failureReason = Resources.CoreManifestFileNamedIncorrectly;
                return null;
            }
        }
        catch (Exception ex) when (ex is Newtonsoft.Json.JsonException or TomlException)
        {
            failureReason = $"Invalid manifest ({manifestLocation}): {ex.Message}";
            return null;
        }

        var mod = new FolderMod
        {
            _name = manifest.Name,
            _author = manifest.Author,
            _version = manifest.Version,
            _location = Path.GetDirectoryName(manifestLocation) ?? "",
            _minimumInstallerVersion = manifest.MinInstallerVersion,
            _manifestVersion = manifest.ManifestVersion,
            _requirements = manifest.Requirements,
            _updateUrl   = manifest.UpdateUrl,
            _downloadUrl = manifest.DownloadUrl
        };

        mod.Validate();
        return mod;
    }

    public Validation Validate()
    {
        if (string.IsNullOrEmpty(_author))
        {
            _validation.Errors.Add(new ValidationMessage(this, Path.Combine(_location, "manifest.json"),
                Resources.CoreManifestHasNoAuthor));
        }

        if (string.IsNullOrEmpty(_name))
        {
            _validation.Errors.Add(new ValidationMessage(this, Path.Combine(_location, "manifest.json"),
                Resources.CoreManifestHasNoName));
        }

        if (string.IsNullOrEmpty(_version))
        {
            _validation.Errors.Add(new ValidationMessage(this, Path.Combine(_location, "manifest.json"),
                Resources.CoreManifestHasNoVersion));
        }

        try
        {
            var currentExe = Assembly.GetEntryAssembly();
            var currentVersionString =
                currentExe!.GetCustomAttribute<AssemblyFileVersionAttribute>()?.Version ?? "0.1.0";
            var currentVersion = new Version(currentVersionString);
            var requiredVersion = new Version(_minimumInstallerVersion);
            var newEngineVersion = new Version("0.12");
            
            if (requiredVersion.CompareTo(newEngineVersion) < 0)
            {
                _validation.Errors.Add(new ValidationMessage(this, Path.Combine(_location, "manifest.json"), Resources.CoreManifestHasNoMinimunInstallerVersion));
            }
            
            // TODO: Remove the workaround for 1.0.0 after the 12th of July
            if (requiredVersion.CompareTo(currentVersion) > 0 && requiredVersion.CompareTo(new Version("1.0")) < 0)
            {
                _validation.Errors.Add(new ValidationMessage(this, Path.Combine(_location, "manifest.json"), Resources.CoreModRequiresNewerInstaller));
            }
        }
        catch (Exception)
        {
            _validation.Errors.Add(new ValidationMessage(this, Path.Combine(_location, "manifest.json"), string.Format(Resources.CoreErrorReadingVersionForMod, GetId())));
        }

        if (new Version(_minimumInstallerVersion).CompareTo(new Version("1.0")) > -1)
        {
            _validation.Warnings.Add(new ValidationMessage(this, Path.Combine(_location, "manifest.json"), Resources.CoreModRequiresIncorrectVersion));
        }
        
        return _validation;
    }

    public static string? GetModLocation(string pathCandidate)
    {
        if (!Directory.Exists(pathCandidate)) return null;
        if (File.Exists(Path.Combine(pathCandidate, "manifest.json")) ||
            File.Exists(Path.Combine(pathCandidate, "manifest.toml"))) return pathCandidate;

        var childFiles = Directory.GetFiles(pathCandidate).Where(file => !file.EndsWith("__folder_managed_by_vortex"))
            .ToArray();
        if (childFiles.Length > 0) return null;

        var children = Directory.GetDirectories(pathCandidate);
        if (children.Length != 1) return null;

        if (File.Exists(Path.Combine(pathCandidate, children[0], "manifest.json")) || File.Exists(Path.Combine(pathCandidate, children[0], "manifest.toml")))
            return Path.Combine(pathCandidate, children[0]);

        return null;
    }

    public bool HasFilesInFolder(string folder, string extension)
    {
        if (!Directory.Exists(Path.Combine(_location, folder))) return false;

        if (!string.IsNullOrEmpty(extension))
            return Directory.GetFiles(Path.Combine(_location, folder), $"*{extension}").Length > 0;
       
        return Directory.GetFiles(Path.Combine(_location, folder)).Length > 0;
    }

    public bool HasFilesInFolder(string folder) => HasFilesInFolder(folder, "");

    public bool FileExists(string path) => File.Exists(Path.Combine(_location, path));

    public bool FolderExists(string path) => Directory.Exists(Path.Combine(_location, path));

    public List<string> GetFilesInFolder(string folder) => GetFilesInFolder(folder, "");

    public List<string> GetFilesInFolder(string folder, string extension)
    {
        if (!Directory.Exists(Path.Combine(_location, folder))) return [];
        if (!string.IsNullOrEmpty(extension))
            return Directory.GetFiles(Path.Combine(_location, folder), $"*{extension}").ToList();
        
        return Directory.GetFiles(Path.Combine(_location, folder)).Where(file => !file.EndsWith("__folder_managed_by_vortex")).ToList();
    }

    public List<string> GetAllFiles(string extension)
    {
        var di = new DirectoryInfo(_location);
        var files = di.GetFiles($"*{extension}", SearchOption.AllDirectories);

        return files.Select(file => file.FullName).ToList();
    }

    public string? ReadFile(string path)
    {
        var fullPath = Path.Combine(_location, path);
        return !File.Exists(fullPath) ? "" : File.ReadAllText(fullPath);
    }

    public Stream ReadFileAsStream(string path)
    {
        return File.OpenRead(Path.Combine(_location, path));
    }
}