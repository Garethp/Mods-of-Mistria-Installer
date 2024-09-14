﻿using Garethp.ModsOfMistriaInstaller.Installer.UMT;
using UndertaleModLib;

namespace Garethp.ModsOfMistriaInstaller.Installer;

public class GraphicsInstaller : IModuleInstaller
{
    public void Install(string fieldsOfMistriaLocation, GeneratedInformation information, Action<string, string> reportStatus)
    {
        if (information.Sprites.Count == 0 && information.Tilesets.Count == 0) return;
        
        var dataFile = new FileInfo(Path.Combine(fieldsOfMistriaLocation, "data.win"));

        reportStatus("Reading Textures/Sprites", "");
        
        using var fileRead = dataFile.OpenRead();
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
        
        using var fileWrite = dataFile.OpenWrite();
        UndertaleIO.Write(fileWrite, gmData);
        fileWrite.Close();
    }
}