using UndertaleModLib.Compiler;

namespace Garethp.ModsOfMistriaInstallerLib.Utils;

public class FileModifier(string fieldsOfMistriaLocation): IFileModifier
{
    private readonly string fieldsOfMistriaLocation = fieldsOfMistriaLocation;

    public bool Exists(string file)
    {
        return Path.Exists(Path.Combine(fieldsOfMistriaLocation, file));
    }

    public string[] FileFiles(string path, string pattern)
    {
        return Directory.GetFiles(Path.Combine(fieldsOfMistriaLocation, path), pattern, SearchOption.AllDirectories);
    }

    public string Read(string file)
    {
        var path = Path.Combine(fieldsOfMistriaLocation, file);
        var extension = Path.GetExtension(path);
        var backupPath = path.Replace(extension, ".bak" + extension);
        
        if (!File.Exists(path))
        {
            throw new FileNotFoundException($"Could not find {file} in Fields of Mistria folder");
        }

        if (!File.Exists(backupPath))
        {
            File.Copy(
                path,
                backupPath
            );
        }

        return File.ReadAllText(backupPath);
    }

    public void Write(string file, string contents)
    {
        File.WriteAllText(Path.Combine(fieldsOfMistriaLocation, file), contents);
    }

    public bool ConditionalRestoreBackup(string file, Func<bool> condition)
    {
        var path = Path.Combine(fieldsOfMistriaLocation, file);
        var extension = Path.GetExtension(path);
        var backupPath = path.Replace(extension, ".bak" + extension);
        
        if (!File.Exists(path))
        {
            throw new FileNotFoundException($"Could not find {file} in Fields of Mistria folder");
        }

        if (!File.Exists(backupPath))
        {
            File.Copy(
                path,
                backupPath
            );
        }
        
        if (condition())
        {
            File.Delete(path);
            File.Copy(backupPath, path);

            return true;
        }

        return false;
    }
}