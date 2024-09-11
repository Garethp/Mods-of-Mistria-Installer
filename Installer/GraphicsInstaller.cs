using Garethp.ModsOfMistriaInstaller.Installer.UMT;
using UndertaleModLib;

namespace Garethp.ModsOfMistriaInstaller.Installer;

public class GraphicsInstaller : IModuleInstaller
{
    public void Install(string fieldsOfMistriaLocation, GeneratedInformation information)
    {
        if (information.Sprites.Count == 0) return;
        
        var dataFile = new FileInfo(Path.Combine(fieldsOfMistriaLocation, "data.win"));

        using var fileRead = dataFile.OpenRead();
        var gmData = UndertaleIO.Read(fileRead);
        fileRead.Close();

        new GraphicsImporter().ImportSpriteData(
            "D:\\SteamLibrary\\steamapps\\common\\RimWorld\\Mods\\FoMInstaller\\olrics_love\\images",
            fieldsOfMistriaLocation, gmData, information.Sprites);
        
        using var fileWrite = dataFile.OpenWrite();
        UndertaleIO.Write(fileWrite, gmData);
        fileWrite.Close();
    }
}