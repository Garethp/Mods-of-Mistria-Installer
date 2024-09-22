using System.Reflection;
using System.Text.RegularExpressions;
using System.Text.RegularExpressions;
using Garethp.ModsOfMistriaInstallerLib.Generator;
using Garethp.ModsOfMistriaInstallerLib.Lang;
using Newtonsoft.Json.Linq;

namespace Garethp.ModsOfMistriaInstallerLib;

public class Mod : IMod
{
    public string Author;

    public string Name;

    public string Version;

    public string Location;

    public string MinimunInstallerVersion;

    public string ManifestVersion;

    public Validation validation = new();
    
    public string Id
    {
        get
        {
            var initialId = $"{Author.ToLower()}.{Name.ToLower()}".Replace(" ", "_");
            return Regex.Replace(initialId, "[^a-zA-Z0-9_\\.]", "");
        }
    }

    public string GetAuthor() => Author;

    public string GetName() => Name;

    public string GetVersion() => Version;

    public string GetLocation() => Location;

    public string GetMinimunInstallerVersion() => MinimunInstallerVersion;

    public string GetManifestVersion() => ManifestVersion;

    public Validation GetValidation() => validation;

    public string GetId() => Id;

    public static Mod FromManifest(string manifestLocation)
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

        var mod = new Mod
        {
            Name = manifest["name"]?.ToString() ?? "",
            Author = manifest["author"]?.ToString() ?? "",
            Version = manifest["version"]?.ToString() ?? "",
            Location = Path.GetDirectoryName(manifestLocation) ?? "",
            MinimunInstallerVersion = manifest["minInstallerVersion"]?.ToString() ?? "0.1.0",
            ManifestVersion = manifest["manifestVersion"]?.ToString() ?? "1",
        };

        mod.Validate();

        return mod;
    }

    public Validation Validate()
    {
        if (string.IsNullOrEmpty(Author))
        {
            validation.Errors.Add(new ValidationMessage(this, Path.Combine(Location, "manifest.json"),
                Resources.ManifestHasNoAuthor));
        }

        if (string.IsNullOrEmpty(Name))
        {
            validation.Errors.Add(new ValidationMessage(this, Path.Combine(Location, "manifest.json"),
                Resources.ManifestHasNoName));
        }

        if (string.IsNullOrEmpty(Version))
        {
            validation.Errors.Add(new ValidationMessage(this, Path.Combine(Location, "manifest.json"),
                Resources.ManifestHasNoVersion));
        }

        return validation;
    }

    public string? CanInstall()
    {
        var currentExe = Assembly.GetEntryAssembly();
        var currentVersionString =
            currentExe!.GetCustomAttribute<AssemblyFileVersionAttribute>()?.Version ?? "0.1.0";
        var currentVersion = new Version(currentVersionString);
        var requiredVersion = new Version(MinimunInstallerVersion);

        if (requiredVersion.CompareTo(currentVersion) > 0)
        {
            return Resources.ModRequiresNewerInstaller;
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
        if (!Directory.Exists(Path.Combine(Location, folder))) return false;

        if (!string.IsNullOrEmpty(extension))
            return Directory.GetFiles(Path.Combine(Location, folder), $"*{extension}").Length > 0;
       
        return Directory.GetFiles(Path.Combine(Location, folder)).Length > 0;
    }

    public bool HasFilesInFolder(string folder) => HasFilesInFolder(folder, "");

    public bool FileExists(string path) => File.Exists(Path.Combine(Location, path));

    public bool FolderExists(string path) => Directory.Exists(Path.Combine(Location, path));

    public List<string> GetFilesInFolder(string folder) => GetFilesInFolder(folder, "");

    public List<string> GetFilesInFolder(string folder, string extension)
    {
        if (!Directory.Exists(Path.Combine(Location, folder))) return new List<string>();
        if (!string.IsNullOrEmpty(extension))
            return Directory.GetFiles(Path.Combine(Location, folder), $"*{extension}").ToList();
        
        return Directory.GetFiles(Path.Combine(Location, folder)).ToList();
    }

    public string? ReadFile(string path)
    {
        return !FileExists(path) ? "" : File.ReadAllText(path);
    }

    public Stream ReadFileAsStream(string path)
    {
        return File.OpenRead(Path.Combine(Location, path));
    }
}