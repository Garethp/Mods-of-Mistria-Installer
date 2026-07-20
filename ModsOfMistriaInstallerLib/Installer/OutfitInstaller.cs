using Garethp.ModsOfMistriaInstallerLib.Generator;
using Garethp.ModsOfMistriaInstallerLib.Models;
using Garethp.ModsOfMistriaInstallerLib.ModTypes;
using Garethp.ModsOfMistriaInstallerLib.Utils;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Tomlyn;
using Tomlyn.Model;

namespace Garethp.ModsOfMistriaInstallerLib.Installer;

// Processes momi/outfit/*.toml compact definitions and generates the data-layer
// content that ImageInstaller/TOMLInstaller can't reach through virtual files:
//   • fiddle/player_assets.toml  — one [id] entry per outfit
//   • data_files/animation/generated/outlines.json  — icon → outline mapping
//   • data_files/animation/generated/player_asset_parts.json  — id → slot → sprite
public class OutfitInstaller(
    Dictionary<string, string> fileNameUidMapping,
    IFileModifier _fileModifier)
    : Installer(fileNameUidMapping)
{
    public override void Install(
        IMod mod, 
        GeneratedInformation generatedInformation,
        Action<string, string> reportStatus
    ) {
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
                InstallPlayerAssetParts(mod, def, reportStatus);
                reportStatus($"Generated outfit data for: {def.Id}", "");
            }
        }
    }

    // ── fiddle/player_assets.toml ─────────────────────────────────────────────

    private void InstallFiddle(OutfitFile def, Action<string, string> reportStatus)
    {
        var dest = DestinationPath("fiddle/player_assets.toml");

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

        if (_fileModifier.Exists(dest))
        {
            var existing = TomlSerializer.Deserialize<TomlTable>(_fileModifier.Read(dest));
            Operations.MOMIOperations.MergeTomlTables(existing, patch);
            _fileModifier.Write(dest, TomlSerializer.Serialize(existing));
        }
        else
        {
            _fileModifier.Write(dest, TomlSerializer.Serialize(patch));
        }
    }


    private void InstallOutlines(OutfitFile def, Action<string, string> reportStatus)
    {
        var dest = DestinationPath("data_files/animation/outlines.json");

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


    private void InstallPlayerAssetParts(IMod mod, OutfitFile def, Action<string, string> reportStatus)
    {
        var dest = DestinationPath("data_files/animation/player_asset_parts.json");

        var parts = OutfitGenerator.GetParts(def.UiSlot);
        JObject itemParts;
        if (parts != null)
        {
            var folder = OutfitGenerator.GetPlayerFolder(def.UiSlot);
            itemParts = new JObject();
            foreach (var part in parts)
            {
                var sprite = $"spr_player_{def.Id}_{part}";
                if (folder != null && !mod.FileExists($"animations/{folder}/{sprite}.png")) continue;
                itemParts[part] = sprite;
            }
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

    private void MergeJson(string destPath, JObject patch)
    {
        if (_fileModifier.Exists(destPath))
        {
            var existing = JObject.Parse(_fileModifier.Read(destPath));
            existing.Merge(patch, new JsonMergeSettings
            {
                MergeArrayHandling    = MergeArrayHandling.Union,
                MergeNullValueHandling = MergeNullValueHandling.Merge,
            });
            _fileModifier.Write(destPath, existing.ToString(Formatting.Indented));
        }
        else
        {
            _fileModifier.Write(destPath, patch.ToString(Formatting.Indented));
        }
    }
}
