namespace Garethp.ModsOfMistriaInstallerLib.Utils;

public interface IFileModifier
{
    public bool Exists(string file);
    
    public string Read(string file);

    public void Write(string file, string contents);

    public string[] FileFiles(string path, string pattern);
    
    public bool ConditionalRestoreBackup(string file, Func<bool> condition);
}