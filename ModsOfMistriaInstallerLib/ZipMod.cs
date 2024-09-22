using System.IO.Compression;
using System.Reflection;
using Garethp.ModsOfMistriaInstallerLib.Generator;
using Garethp.ModsOfMistriaInstallerLib.Lang;
using Newtonsoft.Json.Linq;

namespace Garethp.ModsOfMistriaInstallerLib;

public class ZipMod() : IMod
{
    private string _name = "";

    private string _author = "";

    private string _version = "";

    private string _minimunInstallerVersion = "";

    private string _manifestVersion = "";

    private Validation _validation = new Validation();

    private ZipArchive? _zipFile;

    private string _basePath = "";

    public ZipMod(ZipArchive zipFile, string basePath) : this()
    {
        var manifestFile = zipFile.GetEntry(basePath + "manifest.json");
        if (manifestFile is null) return;

        var manifest = JObject.Parse(readEntry(manifestFile));

        _name = manifest["name"]?.ToString() ?? "";
        _author = manifest["author"]?.ToString() ?? "";
        _version = manifest["version"]?.ToString() ?? "";
        _minimunInstallerVersion = manifest["minInstallerVersion"]?.ToString() ?? "";
        _manifestVersion = manifest["manifestVersion"]?.ToString() ?? "";
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
        Stream entryStream = entry.Open();
        using var reader = new StreamReader(entryStream);
        var contents = reader.ReadToEnd();

        return contents;
    }

    public static ZipMod FromZipFile(string ZipPath)
    {
        var zipMod = new ZipMod();

        if (!File.Exists(ZipPath)) return zipMod;

        var zipFile = ZipFile.OpenRead(ZipPath);

        var manifestFiles = zipFile.Entries.Where(entry => entry.Name == "manifest.json");

        if (manifestFiles.Count() != 1) return zipMod;

        var internalLocation = manifestFiles.First().FullName.Replace("manifest.json", "");

        return new ZipMod(zipFile, internalLocation);
    }

    public string GetAuthor() => _author;

    public string GetName() => _name;

    public string GetVersion() => _version;

    public string GetLocation() => "";

    public string GetMinimunInstallerVersion() => _minimunInstallerVersion;

    public string GetManifestVersion() => _manifestVersion;

    public Validation GetValidation() => _validation;

    public string GetId() => $"{GetAuthor().ToLower()}.{GetName().ToLower()}".Replace(" ", "_");

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

    public bool HasFilesInFolder(string folder) => _zipFile is not null && _zipFile.Entries.Any(entry =>
        entry.FullName.StartsWith($"{_basePath}{folder}") && !entry.FullName.EndsWith('/'));

    public bool FileExists(string path) => _zipFile is not null && _zipFile.Entries.Any(entry => entry.FullName == $"{_basePath}{path}" && !entry.FullName.EndsWith('/'));
    
    public bool FolderExists(string path) => _zipFile?.GetEntry($"{_basePath}{path}/") != null;
    
    public List<string> GetFilesInFolder(string folder) =>
        _zipFile?.Entries.Where(entry => entry.FullName.StartsWith($"{_basePath}{folder}") && !entry.FullName.EndsWith('/')).Select(entry => entry.FullName).ToList() ?? new List<string>();

    public string ReadFile(string path) => readEntry(_zipFile, path);

    public Stream ReadFileAsStream(string path)
    {
        if (_zipFile is null) throw new Exception("Cannot read file from zip file");
        var entry = _zipFile.GetEntry($"{_basePath}{path}");
        if (entry is null) throw new Exception("Cannot read file from zip file");

        return entry.Open();
    }
}