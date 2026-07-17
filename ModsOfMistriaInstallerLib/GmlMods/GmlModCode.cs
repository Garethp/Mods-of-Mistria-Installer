using Garethp.ModsOfMistriaInstallerLib.ModTypes;

namespace Garethp.ModsOfMistriaInstallerLib.GmlMods;

// One behavioural mod's code surface: identity, hook needs and the gml/ tree,
// read through IMod so folder, zip and rar containers behave identically.
public class GmlModCode(IMod mod, string dirName, IReadOnlyList<string> gmlFiles)
{
    public IMod Mod { get; } = mod;

    public string Id { get; } = mod.GetId();

    public string Version { get; } = mod.GetVersion();

    // GML-identifier-safe form of the id: dots and dashes → underscores.
    // Names the install dir assets/gml/scripts/<symbol>/
    public string Symbol { get; } = mod.GetId().Replace('.', '_').Replace('-', '_');

    // The container's own directory name; counts as a lint namespace alongside
    // the symbol, since mods prefix by either
    public string DirName { get; } = dirName;

    public IReadOnlyList<string> RequiredHooks { get; } = mod.GetRequiredHooks();

    // Mod-relative gml paths ("gml/core/State.gml"), ordinal-sorted
    public IReadOnlyList<string> GmlFiles { get; } = gmlFiles;

    public byte[] Read(string rel)
    {
        using var stream = Mod.ReadFileAsStream(rel);
        using var buffer = new MemoryStream();
        stream.CopyTo(buffer);
        return buffer.ToArray();
    }
}
