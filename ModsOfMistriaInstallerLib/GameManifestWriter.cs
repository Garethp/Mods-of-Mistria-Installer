using System.Text.Json;
using System.Text.Json.Serialization;
using Garethp.ModsOfMistriaInstallerLib.ModTypes;

namespace Garethp.ModsOfMistriaInstallerLib;

/// <summary>
/// Writes mods/manifest.json into every active FieldsOfMistria config directory
/// so the in-game Mods tab can read the installed mod list.
/// </summary>
public static class GameManifestWriter
{
    public static void Write(IEnumerable<IMod> mods)
    {
        var entries = mods.Select(m =>
        {
            var entry = new ModEntry
            {
                Id = m.GetId(),
                Name = m.GetName(),
                Version = m.GetVersion(),
            };

            // Best-effort: pull description from the mod's own manifest.json
            try
            {
                var raw = m.ReadFile("manifest.json");
                if (!string.IsNullOrEmpty(raw))
                {
                    var doc = JsonDocument.Parse(raw);
                    if (doc.RootElement.TryGetProperty("description", out var desc))
                        entry.Description = desc.GetString();
                }
            }
            catch { /* description is optional */ }

            return entry;
        }).ToList();

        var payload = new GameManifest { Mods = entries };
        var json = JsonSerializer.Serialize(payload, new JsonSerializerOptions { WriteIndented = true });

        foreach (var configDir in MistriaLocator.GetGameConfigDirectories())
        {
            var modsDir = Path.Combine(configDir, "mods");
            Directory.CreateDirectory(modsDir);
            File.WriteAllText(Path.Combine(modsDir, "manifest.json"), json);
        }
    }

    private class GameManifest
    {
        [JsonPropertyName("mods")]
        public List<ModEntry> Mods { get; set; } = [];
    }

    private class ModEntry
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = "";

        [JsonPropertyName("name")]
        public string Name { get; set; } = "";

        [JsonPropertyName("version")]
        public string Version { get; set; } = "";

        [JsonPropertyName("description")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? Description { get; set; }
    }
}
