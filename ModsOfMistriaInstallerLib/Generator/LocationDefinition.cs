using Tomlyn.Model;
using Garethp.ModsOfMistriaInstallerLib.Utils;

namespace Garethp.ModsOfMistriaInstallerLib.Generator;

// Parsed from a single momi/locations/*.toml file.
// Each [section] defines one new location (section name = location key in locations.toml).
// The section body is the raw TOML table that gets merged into locations.toml as-is,
// so any field supported by the game (outdoor, name, serializable, music, …) is valid.
public class LocationDefinition
{
    public string    Id   { get; init; } = "";
    public TomlTable Data { get; init; } = new();

    // Returns (locationId, definition) for every section in content, skipping "default".
    public static IEnumerable<LocationDefinition> ParseAll(string content)
    {
        var table = Toml.ParseToml(content);
        foreach (var (key, value) in table)
        {
            if (key == "default") continue;
            if (value is not TomlTable data)  continue;

            yield return new LocationDefinition { Id = key, Data = data };
        }
    }
}
