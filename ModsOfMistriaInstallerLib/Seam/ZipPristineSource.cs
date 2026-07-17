using System.IO.Compression;

namespace Garethp.ModsOfMistriaInstallerLib.Seam;

// Read-only accessor over a pristine assets zip (`assets.bak.zip`, or any
// build zip for verify). The central directory is read once into a
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

    public void Dispose() => _archive.Dispose();
}
