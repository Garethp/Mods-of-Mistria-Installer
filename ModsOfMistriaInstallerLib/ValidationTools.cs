using Garethp.ModsOfMistriaInstallerLib.Lang;
using Garethp.ModsOfMistriaInstallerLib.ModTypes;

namespace Garethp.ModsOfMistriaInstallerLib;

public class ValidationTools
{
    public static string? CheckSpriteFileExists(IMod mod, string prefix, string? filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath)) return string.Format(Resources.CoreItemDoesNotHaveValue, prefix);
        if (!mod.FileExists(filePath)) return string.Format(Resources.CoreSpriteFileDoesNotExist, prefix, filePath);
        
        return null;
    }
    
    public static string? CheckSpriteDirectoryExists(IMod mod, string prefix, string? filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath)) return string.Format(Resources.CoreItemDoesNotHaveValue, prefix);
        if (!mod.FolderExists(filePath)) return string.Format(Resources.CoreSpriteFolderDoesNotExist, prefix, filePath);
        if (!mod.HasFilesInFolder(filePath)) return string.Format(Resources.CoreSpriteFolderIsEmpty, prefix, filePath);
        
        return null;
    }
}