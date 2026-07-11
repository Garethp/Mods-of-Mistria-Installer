using Garethp.ModsOfMistriaInstallerLib.Collector;
using Garethp.ModsOfMistriaInstallerLib.Models;
using Garethp.ModsOfMistriaInstallerLib.ModTypes;
using Garethp.ModsOfMistriaInstallerLib.Utils;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using Tomlyn;
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
public class ImageInstaller(
    Dictionary<string, string> fileNameUidMapping,
    AtlasUtilities atlasUtils,
    IFileModifier fileModifier)
    : Installer(fileNameUidMapping)
{
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
            if (frameCount <= 0) frameCount = 1; // frame_len omitted = single frame
            atlasType = Atlas.CanonicalType(atlasType);

            if (metaToml.TryGetValue("meta_properties", out var mpObj) && mpObj is TomlTable mp)
            {
                // replace_id: reuse an existing game texture_id and remove the old atlas entries.
                if (mp.TryGetValue("replace_id", out var repObj) &&
                    repObj is string replaceId && !string.IsNullOrEmpty(replaceId))
                {
                    FileNameUIDMapping[group.BaseName] = replaceId;
                    IDManager.RegisterId(replaceId);
                    atlasUtils.RemoveById(replaceId);
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
            var id = atlasUtils.AddStrip(atlasType, frameWidth, frameHeight, frameCount,
                pngStream, FileNameUIDMapping, group.BaseName);

            reportStatus($"Packed {group.BaseName} → {atlasType} atlas (id {id})", "");
        }
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

            var gameMeta = TomlSerializer.Deserialize<SpriteMetaFile>(fileModifier.Read(gameMetaPath));

            if (gameMeta?.Asset is null)
            {
                reportStatus($"Skipping replacement {spriteName}: couldn't read game animation metadata.", "");
                continue;
            }
            if (gameMeta.Asset.FrameCount <= 0) gameMeta.Asset.FrameCount = 1; // frame_len omitted = single frame

            if (string.IsNullOrEmpty(gameMeta.Meta?.Id))
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
                var modMeta = TomlSerializer.Deserialize<SpriteMetaFile>(mod.ReadFile(modMetaRelPath));
                gameMeta.Merge(modMeta);
            }

            atlasType = Atlas.CanonicalType(atlasType);

            byte[] pngBytes;
            using (var src = mod.ReadFileAsStream(pngPath))
            using (var buffer = new MemoryStream())
            {
                src.CopyTo(buffer);
                pngBytes = buffer.ToArray();
            }

            var pngInfo = Image.Identify(new MemoryStream(pngBytes));
            if (pngInfo.Width != gameMeta.Asset.FrameWidth * gameMeta.Asset.FrameCount || pngInfo.Height != gameMeta.Asset.FrameHeight)
            {
                if (pngInfo.Width % gameMeta.Asset.FrameCount != 0)
                {
                    reportStatus($"Skipping replacement {spriteName}: image width {pngInfo.Width} " +
                                 $"is not divisible by frame count {gameMeta.Asset.FrameCount}.", "");
                    continue;
                }

                gameMeta.Asset.FrameWidth  = pngInfo.Width / gameMeta.Asset.FrameCount;
                gameMeta.Asset.FrameHeight = pngInfo.Height;

                UpdateGameMetaDimensions(gameMeta, gameMetaPath);
                reportStatus($"{spriteName}: resized to {gameMeta.Asset.FrameWidth}×{gameMeta.Asset.FrameHeight} ({gameMeta.Asset.FrameCount} frame(s))", "");
            }

            FileNameUIDMapping[baseName] = gameMeta.Meta.Id;
            IDManager.RegisterId(gameMeta.Meta.Id);
            atlasUtils.RemoveById(gameMeta.Meta.Id);

            using var pngStream = new MemoryStream(pngBytes);
            var id = atlasUtils.AddStrip(gameMeta.Asset.Atlas!, gameMeta.Asset.FrameWidth, gameMeta.Asset.FrameHeight, gameMeta.Asset.FrameCount,
                pngStream, FileNameUIDMapping, baseName);

            reportStatus($"Replaced {spriteName} → {gameMeta.Asset.Atlas} atlas (id {id})", "");
        }
    }
    
    private void UpdateGameMetaDimensions(SpriteMetaFile gameMeta, string gameMetaPath)
    {
        if (gameMeta.Asset is null)
            return;

        gameMeta.Asset.Dimensions = [gameMeta.Asset.FrameWidth * gameMeta.Asset.FrameCount, gameMeta.Asset.FrameHeight];

        fileModifier.Write(gameMetaPath, TomlSerializer.Serialize(gameMeta));
    }

    // Recursively searches assets/animations/ for a meta.toml matching the sprite name.
    private string? FindGameAnimationMetaPath(string spriteName)
    {
        var animationsDir = DestinationPath("animations");
        if (!fileModifier.Exists(animationsDir)) return null;

        return fileModifier.FindFiles(animationsDir, $"{spriteName}.meta.toml").FirstOrDefault();
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

        return !string.IsNullOrEmpty(atlasType) && frameWidth > 0 && frameHeight > 0;
    }
}
