using Garethp.ModsOfMistriaInstallerLib.ModTypes;
using Garethp.ModsOfMistriaInstallerLib.Utils;

namespace Garethp.ModsOfMistriaInstallerLib.Installer;

// Installs .mist files from a mod by overwriting the existing game files.
public class MISTInstaller : Installer
{
    public MISTInstaller(
        string fomLocation,
        InstallManifest manifest,
        Dictionary<string, string> fileNameUIDMapping)
        : base(fomLocation, manifest, fileNameUIDMapping) { }

    public override void Install(IMod mod, Action<string, string> reportStatus)
    {
        var mistFiles = mod.GetAllFiles(".mist")
            .Select(p => RelativePath(mod, p))
            .ToList();

        foreach (var relPath in mistFiles)
            InstallMist(mod, relPath, reportStatus);
    }

    private void InstallMist(IMod mod, string relPath, Action<string, string> reportStatus)
    {
        var dest = DestinationPath(relPath);
        Dirty(dest);

        using var source = mod.ReadFileAsStream(relPath);
        using var destStream = File.Create(dest);
        source.CopyTo(destStream);

        reportStatus($"Installed: {relPath}", "");
    }

    private static string RelativePath(IMod mod, string absolutePath)
    {
        var normalizedBase = mod.GetBasePath().Replace('\\', '/').TrimEnd('/') + '/';
        var normalizedFull = absolutePath.Replace('\\', '/');
        if (normalizedFull.StartsWith(normalizedBase, StringComparison.OrdinalIgnoreCase))
            return normalizedFull[normalizedBase.Length..];
        return normalizedFull;
    }
}