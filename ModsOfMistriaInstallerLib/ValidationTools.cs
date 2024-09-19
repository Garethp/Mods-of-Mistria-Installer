namespace Garethp.ModsOfMistriaInstallerLib;

public class ValidationTools
{
    public static string? CheckSpriteFileExists(Mod mod, string prefix, string? filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath)) return $"{prefix} does not have a value";
        if (!File.Exists(Path.Combine(mod.Location, filePath))) return $"{prefix} points to a sprite at {filePath} but that file does not exist";
        
        return null;
    }
    
    public static string? CheckSpriteDirectoryExists(Mod mod, string prefix, string? filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath)) return $"{prefix} does not have a value";
        if (!Directory.Exists(Path.Combine(mod.Location, filePath))) return $"{prefix} points to a sprite folder at {filePath} but that folder does not exist";
        if (Directory.GetFiles(Path.Combine(mod.Location, filePath)).Length == 0) return $"{prefix} points to a sprite folder at {filePath} but that directory is empty";
        
        return null;
    }
}