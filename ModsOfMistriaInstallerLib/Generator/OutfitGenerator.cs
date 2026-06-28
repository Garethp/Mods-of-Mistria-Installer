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
    private static readonly Dictionary<string, SlotConfig> SlotConfigs = new(StringComparer.OrdinalIgnoreCase)
    {
        ["head_gear"] = new(32, 32, 18, "Player", "Player/Head Accessory", 0.025f, "Left", "Top"),
    };

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
        // Resolve frame dimensions: per-outfit overrides beat slot defaults
        var frameW = def.FrameWidth  ?? slot.FrameWidth;
        var frameH = def.FrameHeight ?? slot.FrameHeight;
        var frameCount = DetectFrameCount(mod, def, slot, frameW);

        // ── Animation meta files ──────────────────────────────────────────────

        // UI item icon (18×18, 1 frame)
        var iconMetaPath = $"animations/Item Icons/Wearable/{def.ResolvedIconSprite}.meta.toml";
        AddIfMissing(mod, out_, iconMetaPath, AnimationMeta(
            frameWidth: 18, frameHeight: 18, frameCount: 1,
            atlas: "Player", duration: 0f,
            offsetH: "Middle", offsetV: "Middle",
            includeAssetKind: true));

        // UI item outline (18×18, 1 frame)
        var outlineMetaPath = $"animations/Item Icons/Wearable/{def.ResolvedOutlineSprite}.meta.toml";
        AddIfMissing(mod, out_, outlineMetaPath, AnimationMeta(
            frameWidth: 18, frameHeight: 18, frameCount: 1,
            atlas: "Player", duration: 0f,
            offsetH: "Middle", offsetV: "Middle",
            includeAssetKind: false));

        // Player outfit animation
        var outfitMetaPath = $"animations/{slot.PlayerFolder}/{def.ResolvedOutfitSprite}.meta.toml";
        AddIfMissing(mod, out_, outfitMetaPath, AnimationMeta(
            frameWidth: frameW, frameHeight: frameH, frameCount: frameCount,
            atlas: slot.Atlas, duration: slot.Duration,
            offsetH: slot.OffsetH, offsetV: slot.OffsetV,
            includeAssetKind: true));

        // Player LUT (11×256, 1 frame)
        var lutMetaPath = $"animations/{slot.PlayerFolder}/{def.ResolvedLutSprite}.meta.toml";
        AddIfMissing(mod, out_, lutMetaPath, AnimationMeta(
            frameWidth: 11, frameHeight: 256, frameCount: 1,
            atlas: slot.Atlas, duration: 0f,
            offsetH: slot.OffsetH, offsetV: slot.OffsetV,
            includeAssetKind: true));

        // ── Shape meta files ──────────────────────────────────────────────────

        var iconShapePath    = $"shapes/Item Icons/Wearable/poly_{def.ResolvedIconSprite[4..]}.meta.toml";
        var outlineShapePath = $"shapes/Item Icons/Wearable/poly_{def.ResolvedOutlineSprite[4..]}.meta.toml";
        var outfitShapePath  = $"shapes/{slot.PlayerFolder}/poly_{def.ResolvedOutfitSprite[4..]}.meta.toml";
        var lutShapePath     = $"shapes/{slot.PlayerFolder}/poly_{def.ResolvedLutSprite[4..]}.meta.toml";

        AddIfMissing(mod, out_, iconShapePath,    ShapeMeta(18,     18,    -9, -9));
        AddIfMissing(mod, out_, outlineShapePath, ShapeMeta(18,     18,    -9, -9));
        AddIfMissing(mod, out_, outfitShapePath,  ShapeMeta(frameW, frameH, 0,  0));
        AddIfMissing(mod, out_, lutShapePath,     ShapeMeta(11,     256,    0,  0));
    }

    // ── TOML builders ─────────────────────────────────────────────────────────

    private static string AnimationMeta(
        int frameWidth, int frameHeight, int frameCount,
        string atlas, float duration,
        string offsetH, string offsetV,
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

    private static int DetectFrameCount(IMod mod, OutfitDefinition def, SlotConfig slot, int frameW)
    {
        var pngPath = $"animations/{slot.PlayerFolder}/{def.ResolvedOutfitSprite}.png";
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

    // Slot-specific metadata: frame dimensions, atlas category, subfolder, animation duration.
    private sealed record SlotConfig(
        int    FrameWidth,
        int    FrameHeight,
        int    DefaultFrameCount,
        string Atlas,
        string PlayerFolder,
        float  Duration,
        string OffsetH,
        string OffsetV);
}
