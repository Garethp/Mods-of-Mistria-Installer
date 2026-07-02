using Garethp.ModsOfMistriaInstallerLib.Generator;
using Garethp.ModsOfMistriaInstallerLib.ModTypes;
using Garethp.ModsOfMistriaInstallerLib.Operations;
using Garethp.ModsOfMistriaInstallerLib.Utils;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Tomlyn;
using Tomlyn.Model;
using Toml = Garethp.ModsOfMistriaInstallerLib.Utils.Toml;

namespace Garethp.ModsOfMistriaInstallerLib.Installer;

// Processes momi/furniture/*.toml compact definitions and generates the data-layer
// content that virtual meta.toml files can't cover:
//   • data_files/animation/generated/outlines.json       — icon → outline mapping
//   • data_files/animation/generated/shadow_manifest.json — mask → shadow mapping
//   • fiddle/items/furniture/<set>.toml                  — item definitions
//   • fiddle/object_prototypes/furniture.toml            — object prototype entries
public class FurnitureInstaller(
    string fomLocation,
    InstallManifest manifest,
    Dictionary<string, string> fileNameUidMapping,
    IFileModifier fileModifier)
    : Installer(fomLocation, manifest, fileNameUidMapping)
{
    public override void Install(IMod mod, Action<string, string> reportStatus)
    {
        if (!mod.HasFilesInFolder("momi/furniture", ".toml"))
            return;

        foreach (var entry in mod.GetFilesInFolder("momi/furniture", ".toml"))
        {
            var relPath = OutfitGenerator.Relativize(mod, entry);
            var content = mod.ReadFile(relPath);
            if (string.IsNullOrWhiteSpace(content)) continue;

            var sourceFileName = Path.GetFileNameWithoutExtension(relPath);
            foreach (var (_, def) in FurnitureDefinition.ParseAll(content, sourceFileName))
            {
                var hasMask = mod.FileExists(
                    $"animations/Placeables/Furniture/{def.Sprite}_mask.png");
                InstallOutlines(def);
                if (hasMask) InstallShadowManifest(def);
                InstallItemFiddle(def);
                InstallObjectPrototype(def);
                reportStatus($"Generated furniture data for: {def.Id}", "");
            }
        }
    }

    // ── data_files/animation/generated/outlines.json ──────────────────────────

    private void InstallOutlines(FurnitureDefinition def)
    {
        var dest = DestinationPath("data_files/animation/generated/outlines.json");
        Dirty(dest);
        MergeJson(dest, new JObject { [def.IconSprite] = def.OutlineSprite });
    }

    // ── data_files/animation/generated/shadow_manifest.json ───────────────────

    private void InstallShadowManifest(FurnitureDefinition def)
    {
        var dest = DestinationPath("data_files/animation/generated/shadow_manifest.json");
        Dirty(dest);
        MergeJson(dest, new JObject { [def.MaskSprite] = def.ShadowSprite });
    }

    // ── fiddle/items/furniture/<set>.toml ─────────────────────────────────────

    private void InstallItemFiddle(FurnitureDefinition def)
    {
        var dest = DestinationPath($"fiddle/items/furniture/{def.SourceFileName}.toml");
        Dirty(dest);

        var entry = new TomlTable
        {
            ["name"]        = def.Name,
            ["description"] = def.Description,
            ["icon_sprite"] = def.IconSprite,
            ["object"]      = def.Id,
        };

        if (def.StoreValue.HasValue)
            entry["value"] = new TomlTable
            {
                ["bin"]   = "self.recipe * 1.1",
                ["store"] = (long)def.StoreValue.Value,
            };

        if (def.RecipeKey is not null)
            entry["recipe_key"] = def.RecipeKey;

        if (def.Tags is not null)
            entry["tags"] = def.Tags;

        if (def.CraftingLevelRequirement.HasValue)
            entry["crafting_level_requirement"] = (long)def.CraftingLevelRequirement.Value;

        if (def.Recipe is not null)
            entry["recipe"] = def.Recipe;

        MergeToml(dest, new TomlTable { [def.Id] = entry });
    }

    // ── fiddle/object_prototypes/furniture.toml ───────────────────────────────

    private void InstallObjectPrototype(FurnitureDefinition def)
    {
        var dest = DestinationPath("fiddle/object_prototypes/furniture.toml");
        Dirty(dest);

        var entry = new TomlTable
        {
            ["size"]  = new TomlArray { (long)def.Size[0], (long)def.Size[1] },
            ["south"] = new TomlTable
            {
                ["sprite"] = def.SpringSprite,
                ["offset"] = new TomlArray { (long)def.ResolvedSouthOffsetX, (long)def.ResolvedSouthOffsetY },
            },
        };

        if (def.RuleGrid is not null)
            entry["rule_grid"] = def.RuleGrid;

        if (def.WindowTiles is not null)
            entry["window_tiles"] = def.WindowTiles;

        if (def.Formation is not null)
            entry["formation"] = def.Formation;

        MergeToml(dest, new TomlTable { [def.Id] = entry });
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private void MergeJson(string destPath, JObject patch)
    {
        if (fileModifier.Exists(destPath))
        {
            var existing = JObject.Parse(fileModifier.Read(destPath));
            existing.Merge(patch, new JsonMergeSettings
            {
                MergeArrayHandling     = MergeArrayHandling.Union,
                MergeNullValueHandling = MergeNullValueHandling.Merge,
            });
            fileModifier.Write(destPath, existing.ToString(Formatting.Indented));
        }
        else
        {
            fileModifier.Write(destPath, patch.ToString(Formatting.Indented));
        }
    }

    private void MergeToml(string destPath, TomlTable patch)
    {
        if (fileModifier.Exists(destPath))
        {
            var existing = TomlSerializer.Deserialize<TomlTable>(fileModifier.Read(destPath));
            MOMIOperations.MergeTomlTables(existing, patch);
            fileModifier.Write(destPath, TomlSerializer.Serialize(existing));
        }
        else
        {
            fileModifier.Write(destPath, TomlSerializer.Serialize(patch));
        }
    }
}
