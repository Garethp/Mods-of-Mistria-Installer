using Garethp.ModsOfMistriaInstallerLib.Generator;

namespace Garethp.ModsOfMistriaInstallerLib.ModTypes;

// Wraps a real IMod and overlays a set of generated virtual files on top.
// GetAllFiles / FileExists / ReadFile transparently include the virtual files,
// so existing installers (ImageInstaller, TOMLInstaller) process them without changes.
//
// Virtual files are keyed by forward-slash relative paths (no base-path prefix).
// GetAllFiles returns them prefixed with GetBasePath() so that TOMLCollector's
// GetRelativePath helper correctly strips the prefix back to a relative path.
public class GeneratedOverlayMod : IMod
{
    private readonly IMod _inner;
    // forward-slash relative path → generated file content
    private readonly IReadOnlyDictionary<string, string> _virtual;

    public GeneratedOverlayMod(IMod inner, IReadOnlyDictionary<string, string> virtualFiles)
    {
        _inner   = inner;
        _virtual = virtualFiles;
    }

    // ── File access (augmented with virtual files) ────────────────────────────

    public List<string> GetAllFiles(string extension)
    {
        var result   = _inner.GetAllFiles(extension).ToList();
        var basePath = NormalizedBase();

        foreach (var (relPath, _) in _virtual)
        {
            if (!relPath.EndsWith(extension, StringComparison.OrdinalIgnoreCase)) continue;
            // Return in the same prefixed format the inner mod uses,
            // so GetRelativePath in collectors can strip the prefix.
            result.Add(string.IsNullOrEmpty(basePath) ? relPath : $"{basePath}/{relPath}");
        }

        return result;
    }

    public bool FileExists(string path)
    {
        if (_virtual.ContainsKey(ToRelative(path))) return true;
        return _inner.FileExists(path);
    }

    public bool FolderExists(string path) => _inner.FolderExists(path);

    public string ReadFile(string path)
    {
        if (_virtual.TryGetValue(ToRelative(path), out var content))
            return content;
        return _inner.ReadFile(path);
    }

    // Streams are for PNG data — virtual files don't have streams, always delegate.
    public Stream ReadFileAsStream(string path) => _inner.ReadFileAsStream(path);

    public bool HasFilesInFolder(string folder, string extension) =>
        _inner.HasFilesInFolder(folder, extension) ||
        _virtual.Keys.Any(k => k.StartsWith(folder.Replace('\\', '/').TrimEnd('/') + '/', StringComparison.OrdinalIgnoreCase)
                             && k.EndsWith(extension, StringComparison.OrdinalIgnoreCase));

    public bool HasFilesInFolder(string folder) => HasFilesInFolder(folder, "");

    public List<string> GetFilesInFolder(string folder, string extension)
    {
        var result  = _inner.GetFilesInFolder(folder, extension).ToList();
        var prefix  = folder.Replace('\\', '/').TrimEnd('/') + '/';
        var basePath = NormalizedBase();

        foreach (var (relPath, _) in _virtual)
        {
            if (!relPath.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)) continue;
            if (!string.IsNullOrEmpty(extension) &&
                !relPath.EndsWith(extension, StringComparison.OrdinalIgnoreCase)) continue;
            result.Add(string.IsNullOrEmpty(basePath) ? relPath : $"{basePath}/{relPath}");
        }

        return result;
    }

    public List<string> GetFilesInFolder(string folder) => GetFilesInFolder(folder, "");

    // Metadata: delegate everything to inner

    public string GetAuthor()                => _inner.GetAuthor();
    public string GetName()                  => _inner.GetName();
    public string GetVersion()               => _inner.GetVersion();
    public string GetLocation()              => _inner.GetLocation();
    public string GetMinimumInstallerVersion() => _inner.GetMinimumInstallerVersion();
    public string GetManifestVersion()       => _inner.GetManifestVersion();
    public Validation GetValidation()        => _inner.GetValidation();
    public string GetId()                    => _inner.GetId();
    public bool IsInstalled()                => _inner.IsInstalled();
    public void SetInstalled(bool installed) => _inner.SetInstalled(installed);
    public Validation Validate()             => _inner.Validate();
    public string GetBasePath()              => _inner.GetBasePath();
    public string? CanInstall()              => _inner.CanInstall();

    // Helpers

    // Strips the base-path prefix to get a forward-slash relative path,
    // matching how virtual file keys are stored.
    private string ToRelative(string path)
    {
        var normalized = path.Replace('\\', '/');
        var prefix     = NormalizedBase();
        if (!string.IsNullOrEmpty(prefix) &&
            normalized.StartsWith(prefix + '/', StringComparison.OrdinalIgnoreCase))
            return normalized[(prefix.Length + 1)..];
        return normalized;
    }

    private string NormalizedBase() =>
        _inner.GetBasePath().Replace('\\', '/').TrimEnd('/');
}
