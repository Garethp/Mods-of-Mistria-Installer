using Garethp.ModsOfMistriaInstallerLib.Lang;
using Garethp.ModsOfMistriaInstallerLib.Lang;

namespace Garethp.ModsOfMistriaInstallerLib;

public class ValidationTools
{
    public static string? CheckSpriteFileExists(Mod mod, string prefix, string? filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath)) return string.Format(Resources.ItemDoesNotHaveValue, prefix);
        if (!File.Exists(Path.Combine(mod.Location, filePath))) return string.Format(Resources.SpriteFileDoesNotExist, prefix, filePath);
        
        return null;
    }
    
    public static string? CheckSpriteDirectoryExists(Mod mod, string prefix, string? filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath)) return string.Format(Resources.ItemDoesNotHaveValue, prefix);
        if (!Directory.Exists(Path.Combine(mod.Location, filePath))) return string.Format(Resources.SpriteFolderDoesNotExist, prefix, filePath);
        if (Directory.GetFiles(Path.Combine(mod.Location, filePath)).Length == 0) return string.Format(Resources.SpriteFolderIsEmpty, prefix, filePath);
        
        return null;
    }
}