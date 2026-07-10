using System.IO.Compression;
using System.Text;
using Garethp.ModsOfMistriaInstallerLib.Utils;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using Tomlyn;
using Tomlyn.Model;

namespace Garethp.ModsOfMistriaInstallerLib;

/// <summary>
/// Generates a synthetic replacement-sprite benchmark mod from a game's assets.zip.
/// </summary>
public static class BenchmarkModGenerator
{
    public static string Generate(string gameDirectory, string modsDirectory, int spriteCount, string modName = "MOMI-Benchmark-Mod")
    {
        var assetsZip = Path.Combine(gameDirectory, "assets.zip");
        if (!File.Exists(assetsZip))
            throw new FileNotFoundException("assets.zip not found", assetsZip);

        var modDir = Path.Combine(modsDirectory, modName);
        var replaceDir = Path.Combine(modDir, "images", "replace");
        Directory.CreateDirectory(replaceDir);

        using var zip = ZipFile.OpenRead(assetsZip);
        var metaEntries = zip.Entries
            .Where(e => e.FullName.StartsWith("assets/animations/", StringComparison.OrdinalIgnoreCase)
                        && e.FullName.EndsWith(".meta.toml", StringComparison.OrdinalIgnoreCase))
            .Take(spriteCount)
            .ToList();

        if (metaEntries.Count == 0)
            throw new InvalidOperationException("No animation meta.toml files found in assets.zip.");

        var generated = 0;
        foreach (var entry in metaEntries)
        {
            using var reader = new StreamReader(entry.Open());
            var metaText = reader.ReadToEnd();
            var meta = Toml.ParseToml(metaText);

            if (!TryReadAnimationMeta(meta, out _, out var frameWidth, out var frameHeight, out var frameCount))
                continue;
            if (frameCount <= 0) frameCount = 1;

            var spriteName = Path.GetFileNameWithoutExtension(
                Path.GetFileNameWithoutExtension(entry.Name));
            var pngPath = Path.Combine(replaceDir, $"{spriteName}.png");

            using var image = new Image<Rgba32>(frameWidth * frameCount, frameHeight);
            image.SaveAsPng(pngPath);
            generated++;
        }

        var manifest = $$"""
        {
          "name": "{{modName}}",
          "version": "1.0.0",
          "author": "MOMI Benchmark",
          "description": "Synthetic replacement-sprite benchmark mod with {{generated}} sprites.",
          "minInstallerVersion": "0.12.0"
        }
        """;

        File.WriteAllText(Path.Combine(modDir, "manifest.json"), manifest, Encoding.UTF8);
        return modDir;
    }

    private static bool TryReadAnimationMeta(
        TomlTable meta,
        out string atlasType,
        out int frameWidth,
        out int frameHeight,
        out int frameCount)
    {
        atlasType = "";
        frameWidth = 0;
        frameHeight = 0;
        frameCount = 0;

        if (!meta.TryGetValue("asset_properties", out var apObj) || apObj is not TomlTable ap)
            return false;

        if (ap.TryGetValue("atlas", out var atlasObj))
            atlasType = atlasObj?.ToString() ?? "";

        if (ap.TryGetValue("frame_size", out var fsObj) &&
            fsObj is TomlArray frameSize && frameSize.Count >= 2)
        {
            frameWidth = Convert.ToInt32(frameSize[0]);
            frameHeight = Convert.ToInt32(frameSize[1]);
        }

        if (ap.TryGetValue("frame_len", out var flObj))
            frameCount = Convert.ToInt32(flObj);

        return !string.IsNullOrEmpty(atlasType) && frameWidth > 0 && frameHeight > 0;
    }
}
