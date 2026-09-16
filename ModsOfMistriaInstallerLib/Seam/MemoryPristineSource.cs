namespace Garethp.ModsOfMistriaInstallerLib.Seam;

// An in-memory pristine tree (entry name → bytes) for every unit fixture.
// Fixtures carry no asset inventory unless a test hands one in, so the asset
// checks stand down for them by default.
public class MemoryPristineSource(IReadOnlyDictionary<string, byte[]> files,
    IReadOnlySet<string>? assetNames = null) : IPristineSource
{
    public bool Has(string entry) => files.ContainsKey(entry);

    public IReadOnlyList<string> Entries(string prefix, string suffix) => files.Keys
        .Where(name => name.StartsWith(prefix, StringComparison.Ordinal)
                       && name.EndsWith(suffix, StringComparison.Ordinal))
        .Order(StringComparer.Ordinal)
        .ToList();

    public byte[]? Read(string entry) => files.TryGetValue(entry, out var bytes) ? bytes : null;

    public IReadOnlyList<string> GmlFiles() => files.Keys
        .Where(n => n.StartsWith("assets/gml/", StringComparison.Ordinal)
                    && n.EndsWith(".gml", StringComparison.Ordinal))
        .Order(StringComparer.Ordinal)
        .ToList();

    public IReadOnlySet<string>? AssetNames() => assetNames;
}
