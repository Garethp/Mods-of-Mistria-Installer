using Garethp.ModsOfMistriaInstallerLib.Models;
using Tomlyn;

namespace Garethp.ModsOfMistriaInstallerLib.Generator;

// Parsed from a single momi/outfit/*.toml file.
// Each [section] in the file defines one outfit; the section name becomes the ID.
// A single file can contain multiple outfits.
public class OutfitDefinition
{
    // Parses all outfit definitions from a TOML file.
    // Each top-level [section] defines one outfit; the key becomes the ID.
    public static Dictionary<string, OutfitFile> ParseAll(string tomlContent)
    {
        var models = new Dictionary<string, OutfitFile>(StringComparer.OrdinalIgnoreCase);
        try
        {
            models = TomlSerializer.Deserialize<Dictionary<string, OutfitFile>>(tomlContent)!;
            foreach (var id in models.Keys)
            {
                models[id].Id = id;
            }
        }
        catch { }

        return models;
    }
}
