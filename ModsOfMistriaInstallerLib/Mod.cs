using System.Reflection;
using Garethp.ModsOfMistriaInstallerLib.Generator;
using Garethp.ModsOfMistriaInstallerLib.Lang;
using Newtonsoft.Json.Linq;

namespace Garethp.ModsOfMistriaInstallerLib;

public class Mod
{
    public string Author;

    public string Name;
    
    public string Version;
    
    public string Location;

    public string MinimunInstallerVersion;

    public string ManifestVersion;

    public Validation validation = new();
    
    public string Id => $"{Author.ToLower()}.{Name.ToLower()}".Replace(" ", "_");

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
            validation.Errors.Add(new ValidationMessage(this, Path.Combine(Location, "manifest.json"), Resources.ManifestHasNoAuthor));
        }
        
        if (string.IsNullOrEmpty(Name))
        {
            validation.Errors.Add(new ValidationMessage(this, Path.Combine(Location, "manifest.json"), Resources.ManifestHasNoName));
        }

        if (string.IsNullOrEmpty(Version))
        {
            validation.Errors.Add(new ValidationMessage(this, Path.Combine(Location, "manifest.json"), Resources.ManifestHasNoVersion));
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
}