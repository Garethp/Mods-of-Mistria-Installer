using Garethp.ModsOfMistriaInstallerLib.ModTypes;
using Garethp.ModsOfMistriaInstallerLib.Models;
using Garethp.ModsOfMistriaInstallerLib.Models.SDK;
using Garethp.ModsOfMistriaInstallerLib.Utils;
using Tomlyn;

namespace Garethp.ModsOfMistriaInstallerLib.Installer;

// Some animations are loaded directly from assets/animations rather than
// exclusively through an atlas. The game's metadata marks these with the
// manual-load tag, so retain the source PNG alongside the generated metadata.
public sealed class ManualLoadInstaller(IFileModifier fileModifier)
{
    public void Install(
        IMod mod,
        GeneratedInformation generatedInformation,
        Action<string, string> reportStatus)
    {
        foreach (var group in generatedInformation.AnimationGroups.Values)
        {
            if (!group.HasAnimation || !group.HasPng) continue;

            var meta = TomlSerializer.Deserialize<SpriteMetaFile>(
                group.AnimationMetaRelPath!.ReadString(mod));
            if (meta?.Asset?.Tags is null ||
                !meta.Asset.Tags.Contains("manual-load", StringComparer.OrdinalIgnoreCase))
                continue;

            var sourcePath = group.PngRelPath!;
            using var source = mod.ReadFileAsStream(sourcePath);
            using var buffer = new MemoryStream();
            source.CopyTo(buffer);

            var destination = Path.Combine(
                "assets",
                sourcePath.Replace('/', Path.DirectorySeparatorChar));
            fileModifier.Write(destination, buffer.ToArray());
            reportStatus($"Installed manual-load image: {sourcePath}", "");
        }
    }
}
