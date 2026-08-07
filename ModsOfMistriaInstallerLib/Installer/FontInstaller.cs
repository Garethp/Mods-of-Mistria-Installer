using Garethp.ModsOfMistriaInstallerLib.ModTypes;
using Garethp.ModsOfMistriaInstallerLib.Utils;

namespace Garethp.ModsOfMistriaInstallerLib.Installer;

// Installs raw font files shipped by a mod. Font metadata is handled by
// TOMLInstaller, but the binary font itself must also be present in assets/.
public sealed class FontInstaller(IFileModifier fileModifier)
{
    public void Install(IMod mod, Action<string, string> reportStatus)
    {
        foreach (var sourcePath in mod.GetAllFiles(".ttf"))
        {
            var relativePath = GetRelativePath(mod, sourcePath);
            var normalizedRelativePath = relativePath.Replace('\\', '/');
            if (!normalizedRelativePath.StartsWith("fonts/", StringComparison.OrdinalIgnoreCase))
                continue;

            using var source = mod.ReadFileAsStream(normalizedRelativePath);
            using var buffer = new MemoryStream();
            source.CopyTo(buffer);

            var destination = Path.Combine(
                "assets",
                normalizedRelativePath.Replace('/', Path.DirectorySeparatorChar));
            fileModifier.Write(destination, buffer.ToArray());
            reportStatus($"Installed font: {normalizedRelativePath}", "");
        }
    }

    private static string GetRelativePath(IMod mod, string path)
    {
        var normalizedPath = path.Replace('\\', '/');
        var basePath = mod.GetBasePath().Replace('\\', '/').TrimEnd('/');
        if (!string.IsNullOrEmpty(basePath) &&
            normalizedPath.StartsWith(basePath + '/', StringComparison.OrdinalIgnoreCase))
        {
            return normalizedPath[(basePath.Length + 1)..];
        }

        return normalizedPath.TrimStart('/');
    }
}
