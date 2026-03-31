namespace Garethp.ModsOfMistriaInstallerLib.Utils;

public interface IFileModifier
{
    public string Read(string fieldsOfMistriaLocation, string file);

    public void Write(string fieldsOfMistriaLocation, string file, string contents);
    
    public bool ConditionalRestoreBackup(string fieldsOfMistriaLocation, string file, Func<bool> condition);
}