using Garethp.ModsOfMistriaInstaller.Installer.UMT;
using UndertaleModLib;

namespace Garethp.ModsOfMistriaInstaller.Installer;

public class GraphicsInstaller : IModuleInstaller
{
    public void Install(string fieldsOfMistriaLocation, GeneratedInformation information)
    {
        // if (information.Sprites.Count == 0) return;
        
        var dataFile = new FileInfo(Path.Combine(fieldsOfMistriaLocation, "data.win"));

        using var fileRead = dataFile.OpenRead();
        var gmData = UndertaleIO.Read(fileRead);
        fileRead.Close();

        var spritesToImport = new List<SpriteData>
        {
            new()
            {
                Name = "spr_player_back_gear_basic_cape2_lut",
                Location = "lut.png",
                HasFrames = false,
                IsPlayerSprite = true,
            },
            new ()
            {
                Name = "spr_ui_item_wearable_back_gear_basic_cape2",
                Location = "ui.png",
                HasFrames = false,
                IsUiSprite = true,
            },
            new ()
            {
                Name = "spr_ui_item_wearable_back_gear_basic_cape2_outline",
                Location = "outline.png",
                HasFrames = false,
                IsUiSprite = true,
            },
            new ()
            {
                Name = "spr_player_back_gear_basic_cape2_back_gear",
                Location = "animation",
                HasFrames = true,
                BoundingBoxMode = 1,
                DeleteCollisionMask = true,
                SpecialType = true,
                SpecialTypeVersion = 3,
                SpecialPlaybackSpeed = 40,
                IsPlayerSprite = true,
            }
        };
        
        new GraphicsImporter().ImportSpriteData(
            "D:\\SteamLibrary\\steamapps\\common\\RimWorld\\Mods\\FoMInstaller\\olrics_love\\images",
            fieldsOfMistriaLocation, gmData, spritesToImport);
        
        using var fileWrite = dataFile.OpenWrite();
        UndertaleIO.Write(fileWrite, gmData);
        fileWrite.Close();
    }
}