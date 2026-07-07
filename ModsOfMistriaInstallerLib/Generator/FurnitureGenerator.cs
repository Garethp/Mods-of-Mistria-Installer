using Garethp.ModsOfMistriaInstallerLib.ModTypes;
using Tomlyn.Model;

namespace Garethp.ModsOfMistriaInstallerLib.Generator;

// Reads momi/furniture/*.toml compact definitions and produces a dictionary of
// generated file contents (keyed by forward-slash relative path).
// These virtual files are consumed by GeneratedOverlayMod so the normal
// ImageInstaller / TOMLInstaller pipeline handles atlas packing and meta writing.
//
// Generated per item (given base sprite spr_furniture_<base>):
//   animations/Placeables/Furniture/spr_furniture_<base>_{spring|mask|shadow}.meta.toml
//   shapes/Placeables/Furniture/poly_furniture_<base>_{spring|mask|shadow}.meta.toml
//   animations/Item Icons/Placeables/Furniture/Modded/spr_ui_item_furniture_<base>{|_outline}.meta.toml
//   shapes/Item Icons/Placeables/Furniture/Modded/poly_ui_item_furniture_<base>{|_outline}.meta.toml
public class FurnitureGenerator
{
    public Dictionary<string, string> Generate(IMod mod)
    {
        var virtual_ = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        if (!mod.HasFilesInFolder("momi/furniture", ".toml"))
            return virtual_;

        foreach (var entry in mod.GetFilesInFolder("momi/furniture", ".toml"))
        {
            var relPath = OutfitGenerator.Relativize(mod, entry);
            var content = mod.ReadFile(relPath);
            if (string.IsNullOrWhiteSpace(content)) continue;

            var fileName = Path.GetFileNameWithoutExtension(relPath);
            foreach (var (_, def) in FurnitureDefinition.ParseAll(content, fileName))
                GenerateFurnitureFiles(mod, def, virtual_);
        }

        return virtual_;
    }

    private static void GenerateFurnitureFiles(
        IMod mod,
        FurnitureDefinition def,
        Dictionary<string, string> out_)
    {
        var w    = def.FrameWidth;
        var h    = def.FrameHeight;
        var oh   = (object)def.OffsetH;
        var ov   = (object)def.OffsetV;
        var base_ = def.SpriteBase; // "furniture_..." (no spr_ prefix)

        // Shape collision bounds for the main sprites. Default: zero offset, full frame size.
        var shapeOx = def.ShapeOffset?[0] ?? 0;
        var shapeOy = def.ShapeOffset?[1] ?? 0;
        var shapeW  = def.ShapeSize?[0]   ?? w;
        var shapeH  = def.ShapeSize?[1]   ?? h;

        // ── spring (always required — every furniture has a spring sprite) ────
        AddIfMissing(mod, out_,
            $"animations/Placeables/Furniture/spr_{base_}_spring.meta.toml",
            AnimationMeta(w, h, "Default", oh, ov));
        AddIfMissing(mod, out_,
            $"shapes/Placeables/Furniture/poly_{base_}_spring.meta.toml",
            ShapeMeta(shapeW, shapeH, shapeOx, shapeOy));

        // ── mask (only for window-type furniture that provides a mask PNG) ────
        if (mod.FileExists($"animations/Placeables/Furniture/spr_{base_}_mask.png"))
        {
            AddIfMissing(mod, out_,
                $"animations/Placeables/Furniture/spr_{base_}_mask.meta.toml",
                AnimationMeta(w, h, "Default", oh, ov));
            AddIfMissing(mod, out_,
                $"shapes/Placeables/Furniture/poly_{base_}_mask.meta.toml",
                ShapeMeta(shapeW, shapeH, shapeOx, shapeOy));
        }

        // ── shadow (only if the mod provides a shadow PNG) ────────────────────
        if (mod.FileExists($"animations/Placeables/Furniture/spr_{base_}_shadow.png"))
        {
            AddIfMissing(mod, out_,
                $"animations/Placeables/Furniture/spr_{base_}_shadow.meta.toml",
                AnimationMeta(w, h, "Shadow", oh, ov));
            AddIfMissing(mod, out_,
                $"shapes/Placeables/Furniture/poly_{base_}_shadow.meta.toml",
                ShapeMeta(shapeW, shapeH, shapeOx, shapeOy));
        }

        // ── icon + outline (atlas = "UI", always 18×18, offset = Middle/Middle) ─
        // def.IconSprite = "spr_ui_item_{base_}"
        // def.OutlineSprite = "spr_ui_item_{base_}_outline"
        // shape uses [4..] to strip "spr_" → "ui_item_{base_}"
        foreach (var iconSprite in new[] { def.IconSprite, def.OutlineSprite })
        {
            AddIfMissing(mod, out_,
                $"animations/Item Icons/Placeables/Furniture/Modded/{iconSprite}.meta.toml",
                AnimationMeta(18, 18, "UI", "Middle", "Middle"));
            AddIfMissing(mod, out_,
                $"shapes/Item Icons/Placeables/Furniture/Modded/poly_{iconSprite[4..]}.meta.toml",
                ShapeMeta(18, 18, -9, -9));
        }
    }

    // ── TOML builders ─────────────────────────────────────────────────────────

    private static string AnimationMeta(int w, int h, string atlas, object offsetH, object offsetV)
    {
        var t = new TomlTable
        {
            ["meta_properties"] = new TomlTable { ["asset_kind"] = "Animation" },
            ["asset_properties"] = new TomlTable
            {
                ["frame_size"] = new TomlArray { w, h },
                ["atlas"]      = atlas,
                ["offset"]     = new TomlTable { ["horizontal"] = offsetH, ["vertical"] = offsetV },
            }
        };
        return Tomlyn.TomlSerializer.Serialize(t);
    }

    private static string ShapeMeta(int w, int h, int ox, int oy)
    {
        var t = new TomlTable
        {
            ["meta_properties"]  = new TomlTable { ["asset_kind"] = "Shape" },
            ["asset_properties"] = new TomlTable
            {
                ["kind"]       = "box",
                ["offset"]     = new TomlArray { ox, oy },
                ["dimensions"] = new TomlArray { w, h },
            }
        };
        return Tomlyn.TomlSerializer.Serialize(t);
    }

    private static void AddIfMissing(IMod mod, Dictionary<string, string> out_, string relPath, string content)
    {
        if (!mod.FileExists(relPath))
            out_[relPath] = content;
    }
}
