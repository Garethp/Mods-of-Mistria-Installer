using Garethp.ModsOfMistriaInstallerLib.Collector;
using Garethp.ModsOfMistriaInstallerLib.ModTypes;
using Garethp.ModsOfMistriaInstallerLib.Utils;
using SixLabors.ImageSharp;
using Tomlyn;
using Tomlyn.Model;

namespace Garethp.ModsOfMistriaInstallerLib.Installer;

public class ImageInstaller(
    string fomLocation,
    InstallManifest manifest,
    Dictionary<string, string> fileNameUidMapping,
    AtlasUtilities atlasUtils,
    IFileModifier fileModifier)
    : Installer(fomLocation, manifest, fileNameUidMapping)
{
    private Dictionary<string, string>? _spriteMetaIndex;

    public override void Install(IMod mod, Action<string, string> reportStatus)
    {
        InstallReplacements(mod, reportStatus);

        var collector = new TOMLCollector();
        collector.Collect(mod);

        var newSpriteReplaceIds = new List<string>();
        var newSpriteJobs = new List<(AnimationGroup Group, string AtlasType, int FrameWidth, int FrameHeight, int FrameCount)>();

        foreach (var group in collector.Groups)
        {
            if (!group.HasAnimation || !group.HasPng) continue;
            if (IsUnderReplaceFolder(group.PngRelPath!)) continue;

            var metaToml = Toml.ParseToml(mod.ReadFile(group.AnimationMetaRelPath!));

            if (!TryReadAnimationMeta(metaToml, out var atlasType, out var frameWidth,
                    out var frameHeight, out var frameCount))
            {
                reportStatus($"Skipping {group.BaseName}: missing animation metadata.", "");
                continue;
            }
            if (frameCount <= 0) frameCount = 1;

            if (metaToml.TryGetValue("meta_properties", out var mpObj) && mpObj is TomlTable mp)
            {
                if (mp.TryGetValue("replace_id", out var repObj) &&
                    repObj is string replaceId && !string.IsNullOrEmpty(replaceId))
                {
                    FileNameUIDMapping[group.BaseName] = replaceId;
                    IDManager.RegisterId(replaceId);
                    newSpriteReplaceIds.Add(replaceId);
                    reportStatus($"Replacing {group.BaseName} (id {replaceId})", "");
                }
                else if (mp.TryGetValue("id", out var presetIdObj) &&
                         presetIdObj is string presetId && !string.IsNullOrEmpty(presetId) &&
                         !FileNameUIDMapping.ContainsKey(group.BaseName))
                {
                    FileNameUIDMapping[group.BaseName] = presetId;
                    IDManager.RegisterId(presetId);
                }
            }

            newSpriteJobs.Add((group, atlasType, frameWidth, frameHeight, frameCount));
        }

        if (newSpriteReplaceIds.Count > 0)
            atlasUtils.RemoveByIds(newSpriteReplaceIds);

        foreach (var (group, atlasType, frameWidth, frameHeight, frameCount) in newSpriteJobs)
        {
            using var pngStream = mod.ReadFileAsStream(group.PngRelPath!);
            var id = atlasUtils.AddStrip(atlasType, frameWidth, frameHeight, frameCount,
                pngStream, FileNameUIDMapping, group.BaseName);

            reportStatus($"Packed {group.BaseName} → {atlasType} atlas (id {id})", "");
        }

        atlasUtils.Flush();
    }

    private void InstallReplacements(IMod mod, Action<string, string> reportStatus)
    {
        if (!mod.HasFilesInFolder("images/replace", ".png")) return;

        var spriteMetaIndex = BuildSpriteMetaIndex();
        var jobs = new List<ReplacementJob>();
        var replaceIds = new List<string>();

        foreach (var pngPath in mod.GetFilesInFolder("images/replace", ".png"))
        {
            var fileName   = Path.GetFileName(pngPath.Replace('\\', '/'));
            var spriteName = Path.GetFileNameWithoutExtension(fileName);
            var baseName   = spriteName.StartsWith("spr_", StringComparison.OrdinalIgnoreCase)
                             ? spriteName[4..] : spriteName;

            using var findScope = InstallProfiler.Measure("ImageInstaller.FindGameAnimationMetaPath");
            InstallProfiler.AddCount("ImageInstaller.FindGameAnimationMetaPath.calls");

            if (!spriteMetaIndex.TryGetValue(spriteName, out var gameMetaPath))
            {
                reportStatus($"Skipping replacement {spriteName}: no matching game sprite found.", "");
                continue;
            }

            var gameMeta = TomlSerializer.Deserialize<TomlTable>(fileModifier.Read(gameMetaPath));

            if (!TryReadAnimationMeta(gameMeta, out var atlasType, out var frameWidth,
                    out var frameHeight, out var frameCount))
            {
                reportStatus($"Skipping replacement {spriteName}: couldn't read game animation metadata.", "");
                continue;
            }
            if (frameCount <= 0) frameCount = 1;

            if (!gameMeta.TryGetValue("meta_properties", out var gmpObj) || gmpObj is not TomlTable gmp ||
                !gmp.TryGetValue("id", out var idObj) || idObj is not string replaceId ||
                string.IsNullOrEmpty(replaceId))
            {
                reportStatus($"Skipping replacement {spriteName}: game meta has no id field.", "");
                continue;
            }

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

            byte[] pngBytes;
            using (var src = mod.ReadFileAsStream(pngPath))
            using (var buffer = new MemoryStream())
            {
                src.CopyTo(buffer);
                pngBytes = buffer.ToArray();
            }

            var pngInfo = Image.Identify(new MemoryStream(pngBytes));
            if (pngInfo.Width != frameWidth * frameCount || pngInfo.Height != frameHeight)
            {
                if (pngInfo.Width % frameCount != 0)
                {
                    reportStatus($"Skipping replacement {spriteName}: image width {pngInfo.Width} " +
                                 $"is not divisible by frame count {frameCount}.", "");
                    continue;
                }

                frameWidth  = pngInfo.Width / frameCount;
                frameHeight = pngInfo.Height;

                UpdateGameMetaDimensions(gameMeta, gameMetaPath, frameWidth, frameHeight, frameCount);
                reportStatus($"{spriteName}: resized to {frameWidth}×{frameHeight} ({frameCount} frame(s))", "");
            }

            jobs.Add(new ReplacementJob(
                spriteName,
                baseName,
                atlasType,
                frameWidth,
                frameHeight,
                frameCount,
                replaceId,
                pngBytes));
        }

        if (jobs.Count == 0) return;

        replaceIds.AddRange(jobs.Select(job => job.ReplaceId));
        atlasUtils.RemoveByIds(replaceIds);

        foreach (var job in jobs)
        {
            FileNameUIDMapping[job.BaseName] = job.ReplaceId;
            IDManager.RegisterId(job.ReplaceId);

            using var pngStream = new MemoryStream(job.PngBytes);
            var id = atlasUtils.AddStrip(job.AtlasType, job.FrameWidth, job.FrameHeight, job.FrameCount,
                pngStream, FileNameUIDMapping, job.BaseName);

            reportStatus($"Replaced {job.SpriteName} → {job.AtlasType} atlas (id {id})", "");
        }
    }

    private Dictionary<string, string> BuildSpriteMetaIndex()
    {
        if (_spriteMetaIndex is not null)
            return _spriteMetaIndex;

        using var _ = InstallProfiler.Measure("ImageInstaller.BuildSpriteMetaIndex");

        var animationsDir = DestinationPath("animations");
        var index = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        if (!fileModifier.Exists(animationsDir))
        {
            _spriteMetaIndex = index;
            return index;
        }

        foreach (var metaPath in fileModifier.FindFiles(animationsDir, ".meta.toml"))
        {
            var spriteName = Path.GetFileNameWithoutExtension(
                Path.GetFileNameWithoutExtension(metaPath.Replace('\\', '/')));
            index.TryAdd(spriteName, metaPath);
        }

        _spriteMetaIndex = index;
        InstallProfiler.AddCount("ImageInstaller.BuildSpriteMetaIndex.entries", index.Count);
        return index;
    }

    private void UpdateGameMetaDimensions(
        TomlTable gameMeta, string gameMetaPath, int frameWidth, int frameHeight, int frameCount)
    {
        if (!gameMeta.TryGetValue("asset_properties", out var apObj) || apObj is not TomlTable ap)
            return;

        ap["frame_size"] = new TomlArray { frameWidth, frameHeight };
        ap["dimensions"] = new TomlArray { frameWidth * frameCount, frameHeight };

        fileModifier.Write(gameMetaPath, TomlSerializer.Serialize(gameMeta));
    }

    private static bool IsUnderReplaceFolder(string relPath) =>
        relPath.Replace('\\', '/').Contains("/replace/", StringComparison.OrdinalIgnoreCase) ||
        relPath.StartsWith("replace/", StringComparison.OrdinalIgnoreCase);

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

    private sealed record ReplacementJob(
        string SpriteName,
        string BaseName,
        string AtlasType,
        int FrameWidth,
        int FrameHeight,
        int FrameCount,
        string ReplaceId,
        byte[] PngBytes);
}
