namespace Garethp.ModsOfMistriaInstallerLib.Seam;

// An in-memory pristine tree (entry name → bytes) for every unit fixture.
public class MemoryPristineSource(IReadOnlyDictionary<string, byte[]> files) : IPristineSource
{
    public bool Has(string entry) => files.ContainsKey(entry);

    public byte[]? Read(string entry) => files.TryGetValue(entry, out var bytes) ? bytes : null;

    public IReadOnlyList<string> GmlFiles() => files.Keys
        .Where(n => n.StartsWith("assets/gml/", StringComparison.Ordinal)
                    && n.EndsWith(".gml", StringComparison.Ordinal))
        .Order(StringComparer.Ordinal)
        .ToList();
}
