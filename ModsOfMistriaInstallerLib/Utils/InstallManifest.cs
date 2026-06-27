using System.Text.Json;
using System.Text.Json.Serialization;

namespace Garethp.ModsOfMistriaInstallerLib.Utils;

public class InstallManifest
{
    private const string ManifestFileName = "momi_manifest.json";

    private readonly string _fomLocation;
    private readonly string _assetsDir;
    private readonly string _backupDir;
    private readonly HashSet<string> _modified = new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> _added = new(StringComparer.OrdinalIgnoreCase);

    private InstallManifest(string fomLocation)
    {
        _fomLocation = fomLocation;
        _assetsDir = Path.Combine(fomLocation, "assets");
        _backupDir = Path.Combine(fomLocation, "assets_backup");
    }

    public static InstallManifest LoadOrCreate(string fomLocation)
    {
        var manifest = new InstallManifest(fomLocation);
        var path = Path.Combine(fomLocation, ManifestFileName);

        if (!File.Exists(path))
            return manifest;

        try
        {
            var data = JsonSerializer.Deserialize<ManifestData>(File.ReadAllText(path));
            if (data is null) return manifest;
            foreach (var p in data.Modified) manifest._modified.Add(p);
            foreach (var p in data.Added) manifest._added.Add(p);
        }
        catch
        {
            // Corrupt manifest — start fresh
        }

        return manifest;
    }

    // Call before writing to a file that already exists.
    // Backs up the original once, then marks it tracked.
    public void TrackModified(string absolutePath)
    {
        var relative = ToRelative(absolutePath);
        if (_modified.Contains(relative)) return;

        var backupPath = Path.Combine(_backupDir, relative);
        Directory.CreateDirectory(Path.GetDirectoryName(backupPath)!);
        // If a backup already exists (e.g., from an incomplete prior install), the original
        // is already preserved there — skip the copy rather than crashing.
        if (!File.Exists(backupPath))
            File.Copy(absolutePath, backupPath);
        _modified.Add(relative);
    }

    // Call before creating a file that does not exist yet.
    public void TrackAdded(string absolutePath)
    {
        _added.Add(ToRelative(absolutePath));
    }

    public bool IsTracked(string absolutePath)
    {
        var relative = ToRelative(absolutePath);
        return _modified.Contains(relative) || _added.Contains(relative);
    }

    public void Save()
    {
        var data = new ManifestData
        {
            Modified = [.. _modified],
            Added = [.. _added]
        };
        File.WriteAllText(
            Path.Combine(_fomLocation, ManifestFileName),
            JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true }));
    }

    // Restores backed-up files, deletes added files, clears the manifest.
    public void Restore()
    {
        foreach (var relative in _modified)
        {
            var backupPath = Path.Combine(_backupDir, relative);
            var assetPath = Path.Combine(_assetsDir, relative);

            if (!File.Exists(backupPath)) continue;
            Directory.CreateDirectory(Path.GetDirectoryName(assetPath)!);
            File.Move(backupPath, assetPath, overwrite: true);
        }

        foreach (var relative in _added)
        {
            var assetPath = Path.Combine(_assetsDir, relative);
            if (File.Exists(assetPath)) File.Delete(assetPath);
        }

        _modified.Clear();
        _added.Clear();

        var manifestPath = Path.Combine(_fomLocation, ManifestFileName);
        if (File.Exists(manifestPath)) File.Delete(manifestPath);
    }

    private string ToRelative(string absolutePath)
    {
        if (absolutePath.StartsWith(_assetsDir, StringComparison.OrdinalIgnoreCase))
            return Path.GetRelativePath(_assetsDir, absolutePath);
        return Path.GetRelativePath(_fomLocation, absolutePath);
    }

    private class ManifestData
    {
        [JsonPropertyName("modified")] public List<string> Modified { get; set; } = [];
        [JsonPropertyName("added")]    public List<string> Added    { get; set; } = [];
    }
}
