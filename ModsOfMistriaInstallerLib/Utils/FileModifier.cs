namespace Garethp.ModsOfMistriaInstallerLib.Utils;

public class FileModifier: IFileModifier
{
    public string Read(string fieldsOfMistriaLocation, string file)
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

    public void Write(string fieldsOfMistriaLocation, string file, string contents)
    {
        File.WriteAllText(Path.Combine(fieldsOfMistriaLocation, file), contents);
    }

    public bool ConditionalRestoreBackup(string fieldsOfMistriaLocation, string file, Func<bool> condition)
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