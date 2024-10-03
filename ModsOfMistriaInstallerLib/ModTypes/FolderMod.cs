using System.Reflection;
using System.Text.RegularExpressions;
using Garethp.ModsOfMistriaInstallerLib.Generator;
using Garethp.ModsOfMistriaInstallerLib.Lang;
using Newtonsoft.Json.Linq;

namespace Garethp.ModsOfMistriaInstallerLib.ModTypes;

public class FolderMod : IMod
{
    private string _author;

    private string _name;

    private string _version;

    private string _location;

    private string _minimunInstallerVersion;

    private string _manifestVersion;

    private Validation _validation = new();
    
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

    public string GetMinimunInstallerVersion() => _minimunInstallerVersion;

    public string GetManifestVersion() => _manifestVersion;

    public Validation GetValidation() => _validation;

    public string GetBasePath() => _location;

    public string GetId() => Id;

    public static FolderMod FromManifest(string manifestLocation)
    {
        if (!File.Exists(manifestLocation))
        {
            throw new FileNotFoundException(Resources.CouldNotFindModManifest);
        }

        if (!manifestLocation.EndsWith("manifest.json"))
        {
            throw new Exception(Resources.ManifestFileNamedIncorrectly);
        }

        var manifest = JObject.Parse(File.ReadAllText(manifestLocation));

        var mod = new FolderMod
        {
            _name = manifest["name"]?.ToString() ?? "",
            _author = manifest["author"]?.ToString() ?? "",
            _version = manifest["version"]?.ToString() ?? "",
            _location = Path.GetDirectoryName(manifestLocation) ?? "",
            _minimunInstallerVersion = manifest["minInstallerVersion"]?.ToString() ?? "0.1.0",
            _manifestVersion = manifest["manifestVersion"]?.ToString() ?? "1"
        };

        mod.Validate();

        return mod;
    }

    public Validation Validate()
    {
        if (string.IsNullOrEmpty(_author))
        {
            _validation.Errors.Add(new ValidationMessage(this, Path.Combine(_location, "manifest.json"),
                Resources.ManifestHasNoAuthor));
        }

        if (string.IsNullOrEmpty(_name))
        {
            _validation.Errors.Add(new ValidationMessage(this, Path.Combine(_location, "manifest.json"),
                Resources.ManifestHasNoName));
        }

        if (string.IsNullOrEmpty(_version))
        {
            _validation.Errors.Add(new ValidationMessage(this, Path.Combine(_location, "manifest.json"),
                Resources.ManifestHasNoVersion));
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
            var requiredVersion = new Version(_minimunInstallerVersion);

            if (requiredVersion.CompareTo(currentVersion) > 0)
            {
                return Resources.ModRequiresNewerInstaller;
            }
        }
        catch (Exception)
        {
            return string.Format(Resources.ErrorReadingVersionForMod, GetId());
        }

        return null;
    }

    public static string? GetModLocation(string pathCandidate)
    {
        if (!Directory.Exists(pathCandidate)) return null;
        if (File.Exists(Path.Combine(pathCandidate, "manifest.json"))) return pathCandidate;

        var childFiles = Directory.GetFiles(pathCandidate);
        if (childFiles.Length > 0) return null;

        var children = Directory.GetDirectories(pathCandidate);
        if (children.Length != 1) return null;

        if (File.Exists(Path.Combine(pathCandidate, children[0], "manifest.json")))
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
        
        return Directory.GetFiles(Path.Combine(_location, folder)).ToList();
    }

    public List<string> GetAllFiles(string extension)
    {
        var di = new DirectoryInfo(_location);
        var files = di.GetFiles($"*{extension}", SearchOption.AllDirectories);

        return files.Select(file => file.FullName).ToList();
    }

    public string? ReadFile(string path)
    {
        return !FileExists(path) ? "" : File.ReadAllText(path);
    }

    public Stream ReadFileAsStream(string path)
    {
        return File.OpenRead(Path.Combine(_location, path));
    }
}