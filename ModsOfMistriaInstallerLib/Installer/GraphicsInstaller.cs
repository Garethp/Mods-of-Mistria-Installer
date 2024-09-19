using Garethp.ModsOfMistriaInstallerLib.Installer.UMT;
using UndertaleModLib;

namespace Garethp.ModsOfMistriaInstallerLib.Installer;

[InformationInstaller(1)]
public class GraphicsInstaller : IModuleInstaller
{
    public void Install(string fieldsOfMistriaLocation, GeneratedInformation information, Action<string, string> reportStatus)
    {
        if (information.Sprites.Count == 0 && information.Tilesets.Count == 0) return;
        
        if (!File.Exists(Path.Combine(fieldsOfMistriaLocation, "data.bak.win")))
        {
            File.Copy(
                Path.Combine(fieldsOfMistriaLocation, "data.win"),
                Path.Combine(fieldsOfMistriaLocation, "data.bak.win")
            );
        }
        else
        {
            // This is stupid. I don't know why we have to do this, but I ran into a bug where somehow just reading from
            // the backup file isn't enough...
            File.Delete(Path.Combine(fieldsOfMistriaLocation, "data.win"));
            File.Copy(
                Path.Combine(fieldsOfMistriaLocation, "data.bak.win"),
                Path.Combine(fieldsOfMistriaLocation, "data.win")
            );
        }
        
        var readDataFile = new FileInfo(Path.Combine(fieldsOfMistriaLocation, "data.bak.win"));
        
        reportStatus("Reading Textures/Sprites", "");
        
        var fileRead = readDataFile.OpenRead();
        var gmData = UndertaleIO.Read(fileRead);
        fileRead.Close();
        
        reportStatus("Importing Textures/Sprites", "");
        var importer = new GraphicsImporter();
        
        foreach (var modName in information.Sprites.Keys)
        {
            importer.ImportSpriteData(fieldsOfMistriaLocation, gmData, information.Sprites[modName], modName);
        }
        
        foreach (var modName in information.Tilesets.Keys)
        {
            importer.ImportTilesetData(fieldsOfMistriaLocation, gmData, information.Tilesets[modName], modName);
        }
        
        reportStatus("Writing Textures/Sprites", "");
        
        var writeDataFile = new FileInfo(Path.Combine(fieldsOfMistriaLocation, "data.win"));
        var fileWrite = writeDataFile.OpenWrite();
        UndertaleIO.Write(fileWrite, gmData);
        fileWrite.Close();
    }
}