// See https://aka.ms/new-console-template for more information

using System.Diagnostics;
using Garethp.ModsOfMistriaInstallerLib.Generator;
using Garethp.ModsOfMistriaInstallerLib.Lang;
using UndertaleModLib;

namespace Garethp.ModsOfMistriaInstallerLib;

public static class Standalone
{
    public static void Run()
    {
        var mistriaLocation = MistriaLocator.GetMistriaLocation();
        if (mistriaLocation == null)
        {
            Logger.Log(Resources.MistriaNotFound);
            return;
        }
        
        var modsLocation = MistriaLocator.GetModsLocation(mistriaLocation);

        if (modsLocation is null || !Directory.Exists(modsLocation))
        {
            Logger.Log(Resources.CouldNotGuessModsAt, Path.Combine(mistriaLocation, "mods"));
            return;
        }

        Logger.Log(Resources.GuessedMistriaAt, mistriaLocation);

        var totalTime = new Stopwatch();
        totalTime.Start();
        
        var installer = new ModInstaller(mistriaLocation, modsLocation);

        var allMods = MistriaLocator.GetMods(mistriaLocation, modsLocation);
        installer.ValidateMods(allMods);
        
        allMods = allMods
            .Where(mod =>
            {
                if (mod.CanInstall() is {} cannotInstall)
                {
                    Logger.Log(cannotInstall);
                    return false;
                }

                if (mod.GetValidation().Status == ValidationStatus.Invalid)
                {
                    Logger.Log(Resources.SkippingModBecauseErrors, mod.GetId());
                    foreach (var error in mod.GetValidation().Errors)
                    {
                        Logger.Log($"  {error.Message}");
                    }
                    
                    return false;
                }

                if (mod.GetValidation().Status == ValidationStatus.Warning)
                {
                    Logger.Log(Resources.ModHasWarnings, mod.GetId());
                    foreach (var warning in mod.GetValidation().Warnings)
                    {
                        Logger.Log($"  {warning.Message}");
                    }
                }
                
                return true;
            })
            .ToList();
        
        installer.InstallMods(allMods, (message, timeTaken) =>
        {
            Logger.Log(Resources.InstalledInReporter, message, timeTaken);
        });
        
        totalTime.Stop();
        Logger.Log(Resources.ModsInstalledInTime, totalTime);
    }

    public static void UnInstall()
    {
        var mistriaLocation = MistriaLocator.GetMistriaLocation();
        if (mistriaLocation == null)
        {
            Logger.Log(Resources.MistriaNotFound);
            return;
        }
        
        var installer = new ModInstaller(mistriaLocation, "");
        installer.Uninstall();
    }
}