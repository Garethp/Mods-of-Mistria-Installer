using Garethp.ModsOfMistriaInstallerLib.Lang;
using Garethp.ModsOfMistriaInstallerLib.ModTypes;

namespace Garethp.ModsOfMistriaInstallerLib;

public class ValidationTools
{
    public static string? CheckSpriteFileExists(IMod mod, string prefix, string? filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath)) return string.Format(Resources.ItemDoesNotHaveValue, prefix);
        if (!mod.FileExists(filePath)) return string.Format(Resources.SpriteFileDoesNotExist, prefix, filePath);
        
        return null;
    }
    
    public static string? CheckSpriteDirectoryExists(IMod mod, string prefix, string? filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath)) return string.Format(Resources.ItemDoesNotHaveValue, prefix);
        if (!mod.FolderExists(filePath)) return string.Format(Resources.SpriteFolderDoesNotExist, prefix, filePath);
        if (!mod.HasFilesInFolder(filePath)) return string.Format(Resources.SpriteFolderIsEmpty, prefix, filePath);
        
        return null;
    }
}