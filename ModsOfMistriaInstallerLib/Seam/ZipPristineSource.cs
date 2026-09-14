using System.IO.Compression;

namespace Garethp.ModsOfMistriaInstallerLib.Seam;

// Read-only accessor over a pristine assets zip (`assets.bak.zip`, or any
// build zip for the seam check). The central directory is read once into a
// name → entry map, so a batch of reads parses it once, not once per entry.
public class ZipPristineSource : IPristineSource, IDisposable
{
    private readonly ZipArchive _archive;
    private readonly Dictionary<string, ZipArchiveEntry> _entries;

    public ZipPristineSource(string zipPath)
    {
        if (!File.Exists(zipPath))
            throw new FileNotFoundException($"pristine backup not found: {zipPath}", zipPath);
        _archive = ZipFile.OpenRead(zipPath);
        _entries = [];
        foreach (var entry in _archive.Entries) _entries[entry.FullName] = entry;
    }

    public bool Has(string entry) => _entries.ContainsKey(entry);

    public byte[]? Read(string entry)
    {
        if (!_entries.TryGetValue(entry, out var archiveEntry)) return null;

        using var stream = archiveEntry.Open();
        using var buffer = new MemoryStream();
        stream.CopyTo(buffer);
        return buffer.ToArray();
    }

    public IReadOnlyList<string> GmlFiles() => _entries.Keys
        .Where(n => n.StartsWith("assets/gml/", StringComparison.Ordinal)
                    && n.EndsWith(".gml", StringComparison.Ordinal))
        .Order(StringComparer.Ordinal)
        .ToList();

    // Sprite and room identifiers are .meta.toml basenames under their asset
    // trees, and object identifiers are the object GML file basenames. The
    // central directory is already in memory, so the inventory is one pass
    // over it, cached on first use.
    public IReadOnlySet<string>? AssetNames()
    {
        if (_assetNames is not null) return _assetNames;

        HashSet<string> names = [];
        foreach (var entry in _entries.Keys)
        {
            var slash = entry.LastIndexOf('/');
            var basename = slash == -1 ? entry : entry[(slash + 1)..];
            if (entry.StartsWith("assets/animations/", StringComparison.Ordinal)
                && basename.StartsWith("spr_", StringComparison.Ordinal)
                && basename.EndsWith(".meta.toml", StringComparison.Ordinal))
                names.Add(basename[..^".meta.toml".Length]);
            else if (entry.StartsWith("assets/tiled/rooms/", StringComparison.Ordinal)
                     && basename.StartsWith("rm_", StringComparison.Ordinal)
                     && basename.EndsWith(".meta.toml", StringComparison.Ordinal))
                names.Add(basename[..^".meta.toml".Length]);
            else if (entry.StartsWith("assets/gml/objects/", StringComparison.Ordinal)
                     && basename.StartsWith("obj_", StringComparison.Ordinal)
                     && basename.EndsWith(".gml", StringComparison.Ordinal))
                names.Add(basename[..^".gml".Length]);
        }

        _assetNames = names;
        return _assetNames;
    }

    private IReadOnlySet<string>? _assetNames;

    public void Dispose() => _archive.Dispose();
}
