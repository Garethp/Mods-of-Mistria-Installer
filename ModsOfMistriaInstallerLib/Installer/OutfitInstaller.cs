using Garethp.ModsOfMistriaInstallerLib.Generator;
using Garethp.ModsOfMistriaInstallerLib.ModTypes;
using Garethp.ModsOfMistriaInstallerLib.Utils;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Tomlyn.Model;

namespace Garethp.ModsOfMistriaInstallerLib.Installer;

// Processes momi/outfit/*.toml compact definitions and generates the data-layer
// content that ImageInstaller/TOMLInstaller can't reach through virtual files:
//   • fiddle/player_assets.toml  — one [id] entry per outfit
//   • data_files/animation/generated/outlines.json  — icon → outline mapping
//   • data_files/animation/generated/player_asset_parts.json  — id → slot → sprite
public class OutfitInstaller : Installer
{
    public OutfitInstaller(
        string fomLocation,
        InstallManifest manifest,
        Dictionary<string, string> fileNameUIDMapping)
        : base(fomLocation, manifest, fileNameUIDMapping) { }

    public override void Install(IMod mod, Action<string, string> reportStatus)
    {
        if (!mod.HasFilesInFolder("momi/outfit", ".toml"))
            return;

        foreach (var entry in mod.GetFilesInFolder("momi/outfit", ".toml"))
        {
            var relPath = OutfitGenerator.Relativize(mod, entry);
            var content = mod.ReadFile(relPath);
            if (string.IsNullOrWhiteSpace(content)) continue;

            foreach (var (_, def) in OutfitDefinition.ParseAll(content))
            {
                InstallFiddle(def, reportStatus);
                InstallOutlines(def, reportStatus);
                InstallPlayerAssetParts(def, reportStatus);
                reportStatus($"Generated outfit data for: {def.Id}", "");
            }
        }
    }

    // ── fiddle/player_assets.toml ─────────────────────────────────────────────

    private void InstallFiddle(OutfitDefinition def, Action<string, string> reportStatus)
    {
        var dest = DestinationPath("fiddle/player_assets.toml");
        Dirty(dest);

        var entry = new TomlTable
        {
            ["name"]             = def.Name,
            ["lut"]              = OutfitGenerator.GetFiddleLutSprite(def),
            ["ui_slot"]          = OutfitGenerator.GetFiddleUiSlot(def.UiSlot),
            ["default_unlocked"] = def.DefaultUnlocked,
            ["ui_sub_category"]  = def.UiSubCategory,
        };
        if (def.PriceOverride.HasValue)
            entry["price_override"] = def.PriceOverride.Value;
        var patch = new TomlTable { [def.Id] = entry };

        if (File.Exists(dest))
        {
            var existing = Toml.LoadToml(dest);
            Operations.MOMIOperations.MergeTomlTables(existing, patch);
            Toml.SaveToml(existing, dest);
        }
        else
        {
            Toml.SaveToml(patch, dest);
        }
    }

    // ── data_files/animation/generated/outlines.json ─────────────────────────

    private void InstallOutlines(OutfitDefinition def, Action<string, string> reportStatus)
    {
        var dest = DestinationPath("data_files/animation/generated/outlines.json");
        Dirty(dest);

        JObject patch;
        if (OutfitGenerator.IsComplexSlot(def.UiSlot))
        {
            // Complex slots (beard/eyes/face_gear/facial_hair/hair):
            // outlines.json maps _merged → _merged_outline
            var mergedSprite  = $"spr_ui_item_wearable_{def.Id}_merged";
            var outlineSprite = def.OutlineSprite ?? $"spr_ui_item_wearable_{def.Id}_merged_outline";
            patch = new JObject { [mergedSprite] = outlineSprite };
        }
        else
        {
            // Simple slots: icon sprite (derived from ID) → outline sprite
            var derivedIconSprite = $"spr_ui_item_wearable_{def.Id}";
            patch = new JObject { [derivedIconSprite] = def.ResolvedOutlineSprite };
        }
        MergeJson(dest, patch);
    }

    // ── data_files/animation/generated/player_asset_parts.json ───────────────

    private void InstallPlayerAssetParts(OutfitDefinition def, Action<string, string> reportStatus)
    {
        var dest = DestinationPath("data_files/animation/generated/player_asset_parts.json");
        Dirty(dest);

        var parts = OutfitGenerator.GetParts(def.UiSlot);
        JObject itemParts;
        if (parts != null)
        {
            // Multi-part slot: write every part using spr_player_{id}_{part} naming
            itemParts = new JObject();
            foreach (var part in parts)
                itemParts[part] = $"spr_player_{def.Id}_{part}";
        }
        else
        {
            // Single-part slot
            itemParts = new JObject
            {
                [OutfitGenerator.GetFiddleSlot(def.UiSlot)] = OutfitGenerator.ResolveOutfitSprite(def)
            };
        }

        MergeJson(dest, new JObject { [def.Id] = itemParts });
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static void MergeJson(string destPath, JObject patch)
    {
        if (File.Exists(destPath))
        {
            var existing = JObject.Parse(File.ReadAllText(destPath));
            existing.Merge(patch, new JsonMergeSettings
            {
                MergeArrayHandling    = MergeArrayHandling.Union,
                MergeNullValueHandling = MergeNullValueHandling.Merge,
            });
            File.WriteAllText(destPath, existing.ToString(Formatting.Indented));
        }
        else
        {
            File.WriteAllText(destPath, patch.ToString(Formatting.Indented));
        }
    }
}
