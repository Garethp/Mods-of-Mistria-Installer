using Garethp.ModsOfMistriaInstallerLib.Models.SDK;
using Garethp.ModsOfMistriaInstallerLib.ModTypes;
using Garethp.ModsOfMistriaInstallerLib.Utils;
using Tomlyn;
using Tomlyn.Model;

namespace Garethp.ModsOfMistriaInstallerLib.Installer;

// Installs .mist files from a mod by overwriting the existing game files.
public class MISTInstaller(
    Dictionary<string, string> fileNameUidMapping,
    IFileModifier _fileModifier)
    : Installer(fileNameUidMapping)
{
    public override void Install(
        IMod mod, 
        GeneratedInformation generatedInformation,
        Action<string, string> reportStatus
    ) {
        foreach (var relPath in generatedInformation.Mist)
            InstallMist(mod, relPath, reportStatus);
    }

    private void InstallMist(IMod mod, FileItem file, Action<string, string> reportStatus)
    {
        var dest = DestinationPath(file.FilePath);
        
        var path = Path.GetDirectoryName(dest);
        var name = Path.GetFileNameWithoutExtension(dest);


        var source = file.ReadString(mod);
        _fileModifier.Write(dest, source);
        
        var metaFile = new MistMetaFile();
        _fileModifier.Write(Path.Combine(path, $"{name}.meta.toml"), TomlSerializer.Serialize(metaFile));

        reportStatus($"Installed: {file.FilePath}", "");
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