namespace Garethp.ModsOfMistriaInstallerLib.Utils;

public interface IFileModifier
{
    public bool Exists(string file);
    
    public string Read(string file);

    public Stream GetReadStream(string file);

    public void Write(string file, string contents);

    public void Write(string file, byte[] contents);

    public Stream GetWriteStream(string file);

    public string[] FindFiles(string path, string pattern);
    
    public bool ConditionalRestoreBackup(string file, Func<bool> condition);
}