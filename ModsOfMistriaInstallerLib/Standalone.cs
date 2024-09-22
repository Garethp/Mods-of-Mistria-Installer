﻿// See https://aka.ms/new-console-template for more information

using System.Diagnostics;
using Garethp.ModsOfMistriaInstallerLib;
using Garethp.ModsOfMistriaInstallerLib.Generator;
using Garethp.ModsOfMistriaInstallerLib.Lang;
using Garethp.ModsOfMistriaInstallerLib.Models;

public class Standalone
{
    public static void Run()
    {
        var mistriaLocation = MistriaLocator.GetMistriaLocation();
        if (mistriaLocation == null)
        {
            Console.WriteLine(Resources.MistriaNotFound);
            return;
        }

        var modsLocation = MistriaLocator.GetModsLocation(mistriaLocation);

        if (modsLocation is null || !Directory.Exists(modsLocation))
        {
            Console.WriteLine(Resources.CouldNotGuessModsAt, Path.Combine(mistriaLocation, "mods"));
            return;
        }

        Console.WriteLine(Resources.GuessedMistriaAt, mistriaLocation);

        var totalTime = new Stopwatch();
        totalTime.Start();
        
        var installer = new ModInstaller(mistriaLocation, modsLocation);

        var mods = Directory
            .GetDirectories(modsLocation)
            .Where(folder => Mod.GetModLocation(folder) is not null)
            .Select(location => Mod.FromManifest(Path.Combine(Mod.GetModLocation(location)!, "manifest.json")))
            .ToList();

        var zipMods = Directory.GetFiles(modsLocation, "*.zip")
            .Select(path => ZipMod.FromZipFile(path))
            .ToList();
        
        List<IMod> allMods = new List<IMod>();
        allMods.AddRange(mods);
        allMods.AddRange(zipMods);

        installer.ValidateMods(allMods);
        
        allMods = allMods
            .Where(mod =>
            {
                if (mod.CanInstall() is not null)
                {
                    Console.WriteLine(Resources.SkippingModBecauseInstallerOld, mod.GetId());
                    return false;
                }

                if (mod.GetValidation().Status == ValidationStatus.Invalid)
                {
                    Console.WriteLine(Resources.SkippingModBecauseErrors, mod.GetId());
                    foreach (var error in mod.GetValidation().Errors)
                    {
                        Console.WriteLine($"  {error.Message}");
                    }
                    
                    return false;
                }

                if (mod.GetValidation().Status == ValidationStatus.Warning)
                {
                    Console.WriteLine(Resources.ModHasWarnings, mod.GetId());
                    foreach (var warning in mod.GetValidation().Warnings)
                    {
                        Console.WriteLine($"  {warning.Message}");
                    }
                }
                
                return true;
            })
            .ToList();
        
        installer.InstallMods(allMods, (message, timeTaken) =>
        {
            Console.WriteLine(Resources.InstalledInReporter, message, timeTaken);
        });
        
        totalTime.Stop();
        Console.WriteLine(Resources.ModsInstalledInTime, totalTime);
    }
}
