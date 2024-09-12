using Garethp.ModsOfMistriaInstaller.Installer.UMT;
using UndertaleModLib;

namespace Garethp.ModsOfMistriaInstaller.Installer;

public class GraphicsInstaller : IModuleInstaller
{
    public void Install(string fieldsOfMistriaLocation, GeneratedInformation information)
    {
        if (information.Sprites.Count == 0) return;
        
        var dataFile = new FileInfo(Path.Combine(fieldsOfMistriaLocation, "data.win"));

        Console.WriteLine("Reading data.win");
        
        using var fileRead = dataFile.OpenRead();
        var gmData = UndertaleIO.Read(fileRead);
        fileRead.Close();

        Console.WriteLine("Importing Sprites");
        var importer = new GraphicsImporter();
        
        foreach (var modName in information.Sprites.Keys)
        {
            importer.ImportSpriteData(fieldsOfMistriaLocation, gmData, information.Sprites[modName], modName);
        }
        
        Console.WriteLine("Writing data.win");
        
        using var fileWrite = dataFile.OpenWrite();
        UndertaleIO.Write(fileWrite, gmData);
        fileWrite.Close();
    }
}