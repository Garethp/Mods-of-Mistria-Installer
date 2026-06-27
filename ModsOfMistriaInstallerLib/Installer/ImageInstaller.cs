using Garethp.ModsOfMistriaInstallerLib.Collector;
using Garethp.ModsOfMistriaInstallerLib.ModTypes;
using Garethp.ModsOfMistriaInstallerLib.Utils;
using Tomlyn.Model;

namespace Garethp.ModsOfMistriaInstallerLib.Installer;

// Handles image strips: reads each spr_*.png from the mod, packs its frames
// into the correct game atlas, and populates FileNameUIDMapping with the
// assigned ID so TOMLInstaller can reference it.
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
        var collector = new TOMLCollector();
        collector.Collect(mod);

        foreach (var group in collector.Groups)
        {
            if (!group.HasAnimation || !group.HasPng)
                continue;

            var metaToml = Toml.ParseToml(mod.ReadFile(group.AnimationMetaRelPath!));

            if (!TryReadAnimationMeta(metaToml, out var atlasType, out var frameWidth,
                    out var frameHeight, out var frameCount))
            {
                reportStatus($"Skipping {group.BaseName}: missing animation metadata.", "");
                continue;
            }

            // If the mod's animation meta already has an id, honour it instead of
            // generating a new one.  This keeps atlas entries and meta files in sync.
            if (metaToml.TryGetValue("meta_properties", out var mpObj) &&
                mpObj is TomlTable mp &&
                mp.TryGetValue("id", out var presetIdObj) &&
                presetIdObj is string presetId &&
                !string.IsNullOrEmpty(presetId) &&
                !FileNameUIDMapping.ContainsKey(group.BaseName))
            {
                FileNameUIDMapping[group.BaseName] = presetId;
                IDManager.RegisterId(presetId);
            }

            using var pngStream = mod.ReadFileAsStream(group.PngRelPath!);
            var id = _atlasUtils.AddStrip(
                atlasType, frameWidth, frameHeight, frameCount,
                pngStream, FileNameUIDMapping, group.BaseName);

            reportStatus($"Packed {group.BaseName} → {atlasType} atlas (id {id})", "");
        }

        _atlasUtils.Flush();
    }

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

        if (!ap.TryGetValue("atlas", out var atlasObj)) return false;
        atlasType = atlasObj?.ToString() ?? "";
        if (string.IsNullOrEmpty(atlasType)) return false;

        if (!ap.TryGetValue("frame_size", out var fsObj) ||
            fsObj is not TomlArray frameSize || frameSize.Count < 2) return false;
        frameWidth  = Convert.ToInt32(frameSize[0]);
        frameHeight = Convert.ToInt32(frameSize[1]);

        if (!ap.TryGetValue("frame_len", out var flObj)) return false;
        frameCount = Convert.ToInt32(flObj);

        return frameWidth > 0 && frameHeight > 0 && frameCount > 0;
    }
}
