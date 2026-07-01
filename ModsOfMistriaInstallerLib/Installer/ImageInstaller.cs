using Garethp.ModsOfMistriaInstallerLib.Collector;
using Garethp.ModsOfMistriaInstallerLib.ModTypes;
using Garethp.ModsOfMistriaInstallerLib.Utils;
using Tomlyn.Model;

namespace Garethp.ModsOfMistriaInstallerLib.Installer;

// Handles image strips: reads each spr_*.png from the mod, packs its frames
// into the correct game atlas, and populates FileNameUIDMapping with the
// assigned ID so TOMLInstaller can reference it.
//
// Two installation modes:
//   • images/            New sprites — auto-generates a unique ID (or uses preset id).
//   • images/replace/    Sprite replacements — looks up the existing game texture_id by
//                        filename, removes old atlas entries, then repacks the replacement
//                        using the same ID so all game references remain valid.
//                        A .meta.toml alongside the PNG is optional; if present it can
//                        override atlas type, frame_size, and frame_len (e.g. different
//                        canvas size). The ID always comes from the game's own meta.
public class ImageInstaller : Installer
{
    private readonly AtlasUtilities _atlasUtils;

    public ImageInstaller(
        string fomLocation,
        InstallManifest manifest,
        Dictionary<string, string> fileNameUIDMapping,
        AtlasUtilities atlasUtils)
        : base(fomLocation, manifest, fileNameUIDMapping)
    {
        _atlasUtils = atlasUtils;
    }

    public override void Install(IMod mod, Action<string, string> reportStatus)
    {
        // --- 1. Sprite replacements (images/replace/) ---
        InstallReplacements(mod, reportStatus);

        // --- 2. New sprites (images/, excluding the replace/ subfolder) ---
        var collector = new TOMLCollector();
        collector.Collect(mod);

        foreach (var group in collector.Groups)
        {
            if (!group.HasAnimation || !group.HasPng) continue;

            // Skip anything that lives inside a replace/ subfolder
            if (IsUnderReplaceFolder(group.PngRelPath!)) continue;

            var metaToml = Toml.ParseToml(mod.ReadFile(group.AnimationMetaRelPath!));

            if (!TryReadAnimationMeta(metaToml, out var atlasType, out var frameWidth,
                    out var frameHeight, out var frameCount))
            {
                reportStatus($"Skipping {group.BaseName}: missing animation metadata.", "");
                continue;
            }

            if (metaToml.TryGetValue("meta_properties", out var mpObj) && mpObj is TomlTable mp)
            {
                // replace_id: reuse an existing game texture_id and remove the old atlas entries.
                if (mp.TryGetValue("replace_id", out var repObj) &&
                    repObj is string replaceId && !string.IsNullOrEmpty(replaceId))
                {
                    FileNameUIDMapping[group.BaseName] = replaceId;
                    IDManager.RegisterId(replaceId);
                    _atlasUtils.RemoveById(replaceId);
                    reportStatus($"Replacing {group.BaseName} (id {replaceId})", "");
                }
                // id: preset ID for a new sprite.
                else if (mp.TryGetValue("id", out var presetIdObj) &&
                         presetIdObj is string presetId && !string.IsNullOrEmpty(presetId) &&
                         !FileNameUIDMapping.ContainsKey(group.BaseName))
                {
                    FileNameUIDMapping[group.BaseName] = presetId;
                    IDManager.RegisterId(presetId);
                }
            }

            using var pngStream = mod.ReadFileAsStream(group.PngRelPath!);
            var id = _atlasUtils.AddStrip(atlasType, frameWidth, frameHeight, frameCount,
                pngStream, FileNameUIDMapping, group.BaseName);

            reportStatus($"Packed {group.BaseName} → {atlasType} atlas (id {id})", "");
        }

        _atlasUtils.Flush();
    }

    // ── Replacement path ──────────────────────────────────────────────────────────

    private void InstallReplacements(IMod mod, Action<string, string> reportStatus)
    {
        if (!mod.HasFilesInFolder("images/replace", ".png")) return;

        foreach (var pngPath in mod.GetFilesInFolder("images/replace", ".png"))
        {
            var fileName   = Path.GetFileName(pngPath.Replace('\\', '/'));
            var spriteName = Path.GetFileNameWithoutExtension(fileName);
            var baseName   = spriteName.StartsWith("spr_", StringComparison.OrdinalIgnoreCase)
                             ? spriteName[4..] : spriteName;

            // Find the game's own animation meta — provides id, atlas, frame_size, frame_len
            var gameMetaPath = FindGameAnimationMetaPath(spriteName);
            if (gameMetaPath is null)
            {
                reportStatus($"Skipping replacement {spriteName}: no matching game sprite found.", "");
                continue;
            }

            var gameMeta = Toml.LoadToml(gameMetaPath);

            if (!TryReadAnimationMeta(gameMeta, out var atlasType, out var frameWidth,
                    out var frameHeight, out var frameCount))
            {
                reportStatus($"Skipping replacement {spriteName}: couldn't read game animation metadata.", "");
                continue;
            }

            if (!gameMeta.TryGetValue("meta_properties", out var gmpObj) || gmpObj is not TomlTable gmp ||
                !gmp.TryGetValue("id", out var idObj) || idObj is not string replaceId ||
                string.IsNullOrEmpty(replaceId))
            {
                reportStatus($"Skipping replacement {spriteName}: game meta has no id field.", "");
                continue;
            }

            // Optional mod meta.toml: can override atlas type, frame_size, frame_len.
            // Also accepts an explicit replace_id to target a different texture_id than
            // the one the game file names (edge case; rarely needed).
            var modMetaRelPath = $"images/replace/{spriteName}.meta.toml";
            if (mod.FileExists(modMetaRelPath))
            {
                var modMeta = Toml.ParseToml(mod.ReadFile(modMetaRelPath));

                if (TryReadAnimationMeta(modMeta, out var oAtlas, out var oW, out var oH, out var oCount))
                {
                    if (!string.IsNullOrEmpty(oAtlas)) atlasType   = oAtlas;
                    if (oW     > 0)                    frameWidth  = oW;
                    if (oH     > 0)                    frameHeight = oH;
                    if (oCount > 0)                    frameCount  = oCount;
                }

                if (modMeta.TryGetValue("meta_properties", out var mmpObj) && mmpObj is TomlTable mmp &&
                    mmp.TryGetValue("replace_id", out var repObj) && repObj is string overrideId &&
                    !string.IsNullOrEmpty(overrideId))
                {
                    replaceId = overrideId;
                }
            }

            FileNameUIDMapping[baseName] = replaceId;
            IDManager.RegisterId(replaceId);
            _atlasUtils.RemoveById(replaceId);

            using var pngStream = mod.ReadFileAsStream(pngPath);
            var id = _atlasUtils.AddStrip(atlasType, frameWidth, frameHeight, frameCount,
                pngStream, FileNameUIDMapping, baseName);

            reportStatus($"Replaced {spriteName} → {atlasType} atlas (id {id})", "");
        }
    }

    // Recursively searches assets/animations/ for a meta.toml matching the sprite name.
    private string? FindGameAnimationMetaPath(string spriteName)
    {
        var animationsDir = Path.Combine(AssetsLocation, "animations");
        if (!Directory.Exists(animationsDir)) return null;

        return Directory
            .GetFiles(animationsDir, $"{spriteName}.meta.toml", SearchOption.AllDirectories)
            .FirstOrDefault();
    }

    private static bool IsUnderReplaceFolder(string relPath) =>
        relPath.Replace('\\', '/').Contains("/replace/", StringComparison.OrdinalIgnoreCase) ||
        relPath.StartsWith("replace/", StringComparison.OrdinalIgnoreCase);

    // ── Shared helpers ────────────────────────────────────────────────────────────

    private static bool TryReadAnimationMeta(
        TomlTable meta,
        out string atlasType,
        out int frameWidth,
        out int frameHeight,
        out int frameCount)
    {
        atlasType   = "";
        frameWidth  = 0;
        frameHeight = 0;
        frameCount  = 0;

        if (!meta.TryGetValue("asset_properties", out var apObj) ||
            apObj is not TomlTable ap) return false;

        if (ap.TryGetValue("atlas", out var atlasObj))
            atlasType = atlasObj?.ToString() ?? "";

        if (ap.TryGetValue("frame_size", out var fsObj) &&
            fsObj is TomlArray frameSize && frameSize.Count >= 2)
        {
            frameWidth  = Convert.ToInt32(frameSize[0]);
            frameHeight = Convert.ToInt32(frameSize[1]);
        }

        if (ap.TryGetValue("frame_len", out var flObj))
            frameCount = Convert.ToInt32(flObj);

        // A partial read (missing some values) is still useful for overrides —
        // callers ignore zero/empty values when applying overrides.
        return !string.IsNullOrEmpty(atlasType) && frameWidth > 0 && frameHeight > 0 && frameCount > 0;
    }
}
