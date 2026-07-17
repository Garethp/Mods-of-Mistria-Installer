namespace Garethp.ModsOfMistriaInstallerLib.Seam;

// Resolves the bundled payload. Logical resource names are repo paths with
// forward slashes, and this is the only type that knows them; the mmapi
// sources and the checker path join here when the install layer lands.
public static class PayloadResolver
{
    private const string CatalogResource = "Seam/Payload/seams.toml";

    private const string MmapiResourcePrefix = "Seam/Payload/mmapi/";

    // The seam catalog: the MOMI_SEAM_CATALOG override when set (a catalog
    // under edit; a missing file fails loudly rather than falling back), else
    // the embedded resource.
    public static (string Name, byte[] Bytes) SeamCatalog()
    {
        var overridePath = Environment.GetEnvironmentVariable("MOMI_SEAM_CATALOG");
        if (!string.IsNullOrEmpty(overridePath))
        {
            if (!File.Exists(overridePath))
                throw new FileNotFoundException(
                    $"MOMI_SEAM_CATALOG points at a missing catalog: {overridePath}", overridePath);
            return (overridePath, File.ReadAllBytes(overridePath));
        }

        return (CatalogResource, ReadResource(CatalogResource));
    }

    // The carried mmapi framework, byte-identical to the pinned commit: every
    // embedded Seam/Payload/mmapi/*.gml, ordinal-sorted by file name. No
    // override: delivery stays byte-exact.
    public static IReadOnlyList<(string Name, byte[] Bytes)> MmapiSources()
    {
        var sources = typeof(PayloadResolver).Assembly.GetManifestResourceNames()
            .Where(name => name.StartsWith(MmapiResourcePrefix, StringComparison.Ordinal))
            .Order(StringComparer.Ordinal)
            .Select(name => (name[MmapiResourcePrefix.Length..], ReadResource(name)))
            .ToList();
        if (sources.Count == 0)
            throw new InvalidOperationException("the embedded mmapi framework is missing");
        return sources;
    }

    private static byte[] ReadResource(string name)
    {
        using var stream = typeof(PayloadResolver).Assembly.GetManifestResourceStream(name)
                           ?? throw new InvalidOperationException($"embedded resource {name} is missing");
        using var buffer = new MemoryStream();
        stream.CopyTo(buffer);
        return buffer.ToArray();
    }
}
