using Garethp.ModsOfMistriaInstallerLib.ModTypes;
using SixLabors.ImageSharp;
using Tomlyn.Model;
using Toml = Garethp.ModsOfMistriaInstallerLib.Utils.Toml;

namespace Garethp.ModsOfMistriaInstallerLib.Generator;

// Reads momi/outfit/*.toml files from a mod and produces a dictionary of
// generated file contents (keyed by forward-slash relative path).
// These virtual files are consumed by GeneratedOverlayMod so that the normal
// ImageInstaller / TOMLInstaller pipeline handles atlas packing and meta writing.
public class OutfitGenerator
{
    // Simple slots: icon + outline (2 UI sprites)
    // Complex slots: asset + body + merged + merged_outline (4 UI sprites)
    //
    // AnimationSuffix: the suffix used in the player animation sprite name when it
    //   differs from the ui_slot value (e.g. beard items use _facial_hair, not _beard).
    //   null = use the ui_slot value as-is.
    // FiddleSlot: the slot key written to player_asset_parts.json when it differs from
    //   the ui_slot value the mod creator writes. null = use the ui_slot value as-is.
    // HasLut: whether this slot type uses a LUT sprite. Some slots (beard, facial_hair)
    //   have no LUT; the LUT is still generated if the mod creator explicitly sets one.
    private static readonly Dictionary<string, SlotConfig> SlotConfigs = new(StringComparer.OrdinalIgnoreCase)
    {
        ["back_gear"]   = new(32, 32, 59, "Player", "Player/Back Accessory",   0.025f, "Left", "Top", AnimationSuffix: "back_gear",   FiddleSlot: "back_gear",  FiddleUiSlot: "back"),
        ["beard"]       = new(16, 16, 22, "Player", "Player/Facial Hair",      0.025f, "Left", "Top", ComplexIcons: true,  HasLut: false, AnimationSuffix: "facial_hair", FiddleSlot: "facial_hair", FiddleUiSlot: "facial_hair", SharedLutSprite: "spr_player_hair_lut"),
        ["dress"]       = new(32, 32, 49, "Player", "Player/Dress",            0.025f, "Left", "Top", FiddleUiSlot: "top",      Parts: ["torso", "sleeve_left", "sleeve_right", "waist"]),
        ["eyes"]        = new(16, 16, 13, "Player", "Player/Eyes",             0.025f, "Left", "Top", ComplexIcons: true,              AnimationSuffix: "eyes",        FiddleSlot: "eyes",       FiddleUiSlot: "eyes"),
        ["face_gear"]   = new(32, 32, 20, "Player", "Player/Face Accessory",   0.025f, "Left", "Top", ComplexIcons: true,              AnimationSuffix: "face_gear",   FiddleSlot: "face_gear",  FiddleUiSlot: "face_gear"),
        ["facial_hair"] = new(16, 16, 22, "Player", "Player/Facial Hair",      0.025f, "Left", "Top", ComplexIcons: true,  HasLut: false, AnimationSuffix: "facial_hair", FiddleSlot: "facial_hair", FiddleUiSlot: "facial_hair", SharedLutSprite: "spr_player_hair_lut"),
        ["hair"]        = new(40, 40, 49, "Player", "Player/Hair",             0.025f, "Left", "Top", ComplexIcons: true,              FiddleUiSlot: "hair",     Parts: ["hair_back", "hair_mid"]),
        ["head"]        = new(32, 32, 18, "Player", "Player/Head Accessories", 0.025f, "Left", "Top"),
        ["head_gear"]   = new(32, 32, 18, "Player", "Player/Head Accessory",   0.025f, "Left", "Top", AnimationSuffix: "head_gear",   FiddleSlot: "head_gear",  FiddleUiSlot: "head_gear"),
        ["overalls"]    = new(32, 32, 48, "Player", "Player/Overalls",         0.025f, "Left", "Top", FiddleUiSlot: "top",      Parts: ["torso", "sleeve_left", "sleeve_right", "legs"]),
        ["pants"]       = new(32, 32, 48, "Player", "Player/Pants",            0.025f, "Left", "Top", AnimationSuffix: "legs",        FiddleSlot: "legs",       FiddleUiSlot: "bottom"),
        ["robe"]        = new(32, 32, 49, "Player", "Player/Robes and Coats",  0.025f, "Left", "Top", FiddleUiSlot: "top",      Parts: ["torso", "sleeve_left", "sleeve_right", "waist"]),
        ["shoes"]       = new(32, 32, 41, "Player", "Player/Shoes",            0.025f, "Left", "Top", AnimationSuffix: "feet",        FiddleSlot: "feet",       FiddleUiSlot: "feet"),
        ["shorts"]      = new(32, 32, 48, "Player", "Player/Pants",            0.025f, "Left", "Top", AnimationSuffix: "legs",        FiddleSlot: "legs",       FiddleUiSlot: "bottom"),
        ["skirt"]       = new(32, 32, 48, "Player", "Player/Skirts",           0.025f, "Left", "Top", AnimationSuffix: "waist",       FiddleSlot: "waist",      FiddleUiSlot: "bottom"),
        ["skin"]        = new(32, 32, 49, "Player", "Player/Base",             0.025f, "Left", "Top"),
        ["suit"]        = new(32, 32, 48, "Player", "Player/Suits",            0.025f, "Left", "Top", FiddleUiSlot: "top",      Parts: ["torso", "sleeve_left", "sleeve_right", "legs"]),
        ["top"]         = new(32, 32, 49, "Player", "Player/Tops",             0.025f, "Left", "Top", FiddleUiSlot: "top",      Parts: ["torso", "sleeve_left", "sleeve_right"]),
        ["underwear"]   = new(32, 32, 48, "Player", "Player/Underwear",        0.025f, "Left", "Top", AnimationSuffix: "legs",        FiddleSlot: "legs",       FiddleUiSlot: "bottom"),
    };

    // Used by OutfitInstaller to know which outlines.json pattern to write.
    internal static bool IsComplexSlot(string uiSlot) =>
        SlotConfigs.TryGetValue(uiSlot, out var cfg) && cfg.ComplexIcons;

    // The slot key written to player_asset_parts.json (may differ from ui_slot).
    internal static string GetFiddleSlot(string uiSlot) =>
        SlotConfigs.TryGetValue(uiSlot, out var cfg) ? (cfg.FiddleSlot ?? uiSlot) : uiSlot;

    // The ui_slot value written to player_assets.toml (the game's internal enum, may differ from
    // the momi ui_slot the mod author writes — e.g. "back_gear" → "back", "pants" → "bottom").
    internal static string GetFiddleUiSlot(string uiSlot) =>
        SlotConfigs.TryGetValue(uiSlot, out var cfg) ? (cfg.FiddleUiSlot ?? uiSlot) : uiSlot;

    // The lut sprite to write in player_assets.toml for this item.
    // Priority: explicit mod override → slot SharedLutSprite → derived per-item name.
    internal static string GetFiddleLutSprite(OutfitDefinition def) =>
        def.LutSprite
        ?? (SlotConfigs.TryGetValue(def.UiSlot, out var cfg) ? cfg.SharedLutSprite : null)
        ?? $"spr_player_{def.Id}_lut";

    // The outfit sprite name for single-part slots (ignores multi-part slots).
    internal static string ResolveOutfitSprite(OutfitDefinition def) =>
        def.OutfitSprite ?? $"spr_player_{def.Id}_{GetAnimSuffix(def.UiSlot)}";

    private static string GetAnimSuffix(string uiSlot) =>
        SlotConfigs.TryGetValue(uiSlot, out var cfg) ? (cfg.AnimationSuffix ?? uiSlot) : uiSlot;

    // Returns the parts array for multi-part slots, null for single-part slots.
    internal static string[]? GetParts(string uiSlot) =>
        SlotConfigs.TryGetValue(uiSlot, out var cfg) ? cfg.Parts : null;

    // Builds virtual file contents from the mod's momi/outfit/ definitions.
    // Only generates files the mod hasn't already provided.
    public Dictionary<string, string> Generate(IMod mod)
    {
        var virtual_ = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        if (!mod.HasFilesInFolder("momi/outfit", ".toml"))
            return virtual_;

        foreach (var entry in mod.GetFilesInFolder("momi/outfit", ".toml"))
        {
            var relPath = Relativize(mod, entry);
            var content = mod.ReadFile(relPath);
            if (string.IsNullOrWhiteSpace(content)) continue;

            foreach (var (_, def) in OutfitDefinition.ParseAll(content))
            {
                if (!SlotConfigs.TryGetValue(def.UiSlot, out var slot)) continue;
                GenerateOutfitFiles(mod, def, slot, virtual_);
            }
        }

        return virtual_;
    }

    private static void GenerateOutfitFiles(
        IMod mod,
        OutfitDefinition def,
        SlotConfig slot,
        Dictionary<string, string> out_)
    {
        var frameW       = def.FrameWidth  ?? slot.FrameWidth;
        var frameH       = def.FrameHeight ?? slot.FrameHeight;
        var outfitSprite = ResolveOutfitSprite(def);
        var frameCount   = DetectFrameCount(mod, slot, outfitSprite, frameW);

        // ── UI icon files ─────────────────────────────────────────────────────

        if (slot.ComplexIcons)
        {
            // asset / body / merged / merged_outline
            foreach (var suffix in new[] { "_asset", "_body", "_merged", "_merged_outline" })
            {
                var iconPath  = $"animations/Item Icons/Wearable/{def.ResolvedIconSprite}{suffix}.meta.toml";
                var shapePath = $"shapes/Item Icons/Wearable/poly_{def.ResolvedIconSprite[4..]}{suffix}.meta.toml";
                AddIfMissing(mod, out_, iconPath,  AnimationMeta(18, 18, 1, "UI", 0f, 9.0, 9.0, true));
                AddIfMissing(mod, out_, shapePath, ShapeMeta(18, 18, -9, -9));
            }
        }
        else
        {
            // icon + outline
            var iconMetaPath    = $"animations/Item Icons/Wearable/{def.ResolvedIconSprite}.meta.toml";
            var outlineMetaPath = $"animations/Item Icons/Wearable/{def.ResolvedOutlineSprite}.meta.toml";
            AddIfMissing(mod, out_, iconMetaPath,    AnimationMeta(18, 18, 1, "UI", 0f, 9.0, 9.0, true));
            AddIfMissing(mod, out_, outlineMetaPath, AnimationMeta(18, 18, 1, "UI", 0f, 9.0, 9.0, true));

            var iconShapePath    = $"shapes/Item Icons/Wearable/poly_{def.ResolvedIconSprite[4..]}.meta.toml";
            var outlineShapePath = $"shapes/Item Icons/Wearable/poly_{def.ResolvedOutlineSprite[4..]}.meta.toml";
            AddIfMissing(mod, out_, iconShapePath,    ShapeMeta(18, 18, -9, -9));
            AddIfMissing(mod, out_, outlineShapePath, ShapeMeta(18, 18, -9, -9));
        }

        // ── Player animation ──────────────────────────────────────────────────

        if (slot.Parts is { } parts)
        {
            // Multi-part slot: generate a meta for every declared part.
            // Frame count is detected from the first part PNG found in the mod,
            // then shared across all parts (they all have the same animation length).
            var multiFrameCount = slot.DefaultFrameCount;
            foreach (var part in parts)
            {
                var ps = $"spr_player_{def.Id}_{part}";
                if (mod.FileExists($"animations/{slot.PlayerFolder}/{ps}.png"))
                {
                    multiFrameCount = DetectFrameCount(mod, slot, ps, frameW);
                    break;
                }
            }
            foreach (var part in parts)
            {
                var ps = $"spr_player_{def.Id}_{part}";
                AddIfMissing(mod, out_, $"animations/{slot.PlayerFolder}/{ps}.meta.toml",
                    AnimationMeta(frameW, frameH, multiFrameCount, slot.Atlas, slot.Duration,
                        slot.OffsetH, slot.OffsetV, includeAssetKind: true));
                AddIfMissing(mod, out_, $"shapes/{slot.PlayerFolder}/poly_{ps[4..]}.meta.toml",
                    ShapeMeta(frameW, frameH, 0, 0));
            }
        }
        else
        {
            // Single-part slot
            var outfitMetaPath  = $"animations/{slot.PlayerFolder}/{outfitSprite}.meta.toml";
            var outfitShapePath = $"shapes/{slot.PlayerFolder}/poly_{outfitSprite[4..]}.meta.toml";
            AddIfMissing(mod, out_, outfitMetaPath, AnimationMeta(
                frameW, frameH, frameCount, slot.Atlas, slot.Duration,
                slot.OffsetH, slot.OffsetV, includeAssetKind: true));
            AddIfMissing(mod, out_, outfitShapePath, ShapeMeta(frameW, frameH, 0, 0));
        }

        // ── LUT ──────────────────────────────────────────────────────────────────
        // Only generate meta.toml when the mod actually provides the LUT PNG.
        // Some items reference shared game-provided LUTs (e.g. pants → top_lut) that
        // are already in the atlas; those must not be re-packed from a missing PNG.

        var lutSprite   = def.LutSprite ?? $"spr_player_{def.Id}_lut";
        var lutPngPath  = $"animations/{slot.PlayerFolder}/{lutSprite}.png";
        var lutInMod    = mod.FileExists(lutPngPath);
        if (lutInMod)
        {
            var (lutW, lutH) = DetectLutDimensions(mod, slot, lutSprite);
            var lutMetaPath  = $"animations/{slot.PlayerFolder}/{lutSprite}.meta.toml";
            var lutShapePath = $"shapes/{slot.PlayerFolder}/poly_{lutSprite[4..]}.meta.toml";
            AddIfMissing(mod, out_, lutMetaPath,  AnimationMeta(lutW, lutH, 1, slot.Atlas, 0f, "Middle", "Middle", true));
            AddIfMissing(mod, out_, lutShapePath, ShapeMeta(lutW, lutH, 0, 0));
        }
    }

    // ── TOML builders ─────────────────────────────────────────────────────────

    private static string AnimationMeta(
        int frameWidth, int frameHeight, int frameCount,
        string atlas, float duration,
        object offsetH, object offsetV,
        bool includeAssetKind)
    {
        var t = new TomlTable();
        if (includeAssetKind)
            t["meta_properties"] = new TomlTable { ["asset_kind"] = "Animation" };

        var ap = new TomlTable
        {
            ["frame_size"]         = new TomlArray { frameWidth, frameHeight },
            ["frame_len"]          = frameCount,
            ["dimensions"]         = new TomlArray { frameWidth * frameCount, frameHeight },
            ["filter_kind"]        = "Nearest",
            ["mipmap_filter_kind"] = "Nearest",
            ["wrap"]               = "Repeat",
            ["duration"]           = (double)duration,
            ["atlas"]              = atlas,
            ["offset"]             = new TomlTable { ["horizontal"] = offsetH, ["vertical"] = offsetV },
        };
        t["asset_properties"] = ap;
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

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static int DetectFrameCount(IMod mod, SlotConfig slot, string outfitSprite, int frameW)
    {
        var pngPath = $"animations/{slot.PlayerFolder}/{outfitSprite}.png";
        if (!mod.FileExists(pngPath)) return slot.DefaultFrameCount;

        try
        {
            using var stream = mod.ReadFileAsStream(pngPath);
            var info = Image.Identify(stream);
            return Math.Max(1, info.Width / frameW);
        }
        catch
        {
            return slot.DefaultFrameCount;
        }
    }

    private static (int Width, int Height) DetectLutDimensions(IMod mod, SlotConfig slot, string lutSprite)
    {
        var pngPath = $"animations/{slot.PlayerFolder}/{lutSprite}.png";
        if (!mod.FileExists(pngPath)) return (5, 256);

        try
        {
            using var stream = mod.ReadFileAsStream(pngPath);
            var info = Image.Identify(stream);
            return (Math.Max(1, info.Width), Math.Max(1, info.Height));
        }
        catch
        {
            return (5, 256);
        }
    }

    private static void AddIfMissing(IMod mod, Dictionary<string, string> out_, string relPath, string content)
    {
        if (!mod.FileExists(relPath))
            out_[relPath] = content;
    }

    internal static string Relativize(IMod mod, string path)
    {
        var normalizedBase = mod.GetBasePath().Replace('\\', '/').TrimEnd('/') + '/';
        var normalizedPath = path.Replace('\\', '/');
        if (normalizedPath.StartsWith(normalizedBase, StringComparison.OrdinalIgnoreCase))
            return normalizedPath[normalizedBase.Length..];
        return normalizedPath;
    }

    private sealed record SlotConfig(
        int     FrameWidth,
        int     FrameHeight,
        int     DefaultFrameCount,
        string  Atlas,
        string  PlayerFolder,
        float   Duration,
        string  OffsetH,
        string  OffsetV,
        bool    ComplexIcons     = false,
        bool    HasLut           = true,
        string? AnimationSuffix  = null,
        string? FiddleSlot       = null,
        string? FiddleUiSlot     = null,
        // When set: written as the lut value in player_assets.toml for all items in this slot
        // regardless of the item ID.  No per-item LUT meta.toml is generated.
        // Mod authors can still override with an explicit lut = "..." in their outfit TOML.
        string? SharedLutSprite  = null,
        // When set: this slot has multiple animation parts (e.g. torso, sleeve_left, legs).
        // Each part generates its own animation meta.toml using spr_player_{id}_{part} naming,
        // and all parts are written to player_asset_parts.json.
        // AnimationSuffix and FiddleSlot are ignored when Parts is set.
        string[]? Parts          = null);
}
