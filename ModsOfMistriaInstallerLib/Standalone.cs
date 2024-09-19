// See https://aka.ms/new-console-template for more information

using System.Diagnostics;
using Garethp.ModsOfMistriaInstallerLib;
using Garethp.ModsOfMistriaInstallerLib.Generator;

public class Standalone
{
    public static void Run()
    {
        var mistriaLocation = MistriaLocator.GetMistriaLocation();
        if (mistriaLocation == null)
        {
            Console.WriteLine("Could not find Fields of Mistria location.");
            return;
        }

        var modsLocation = MistriaLocator.GetModsLocation(mistriaLocation);

        if (modsLocation is null || !Directory.Exists(modsLocation))
        {
            Console.WriteLine($"Could not find a mods folder at {Path.Combine(mistriaLocation, "mods")}.");
            return;
        }

        Console.WriteLine($"Guessed Location: {mistriaLocation}");

        var totalTime = new Stopwatch();
        totalTime.Start();
        
        var installer = new ModInstaller(mistriaLocation);

        var mods = Directory
            .GetDirectories(modsLocation)
            .Where(folder => File.Exists(Path.Combine(folder, "manifest.json")))
            .Select(location => Mod.FromManifest(Path.Combine(location, "manifest.json")))
            .ToList();

        installer.ValidateMods(mods);
        
        mods = mods
            .Where(mod =>
            {
                if (mod.CanInstall() != "")
                {
                    Console.WriteLine($"Skipping {mod.Id} as it requires a newer version of the installer.");
                }

                if (mod.validation.Status == ValidationStatus.Invalid)
                {
                    Console.WriteLine($"Skipping {mod.Id} for the following Errors:");
                    foreach (var error in mod.validation.Errors)
                    {
                        Console.WriteLine($"  {error.Message}");
                    }
                    
                    return false;
                }

                if (mod.validation.Status == ValidationStatus.Warning)
                {
                    Console.WriteLine($"{mod.Id} has the following warnings, but will still install:");
                    foreach (var warning in mod.validation.Warnings)
                    {
                        Console.WriteLine($"  {warning.Message}");
                    }
                }
                
                return true;
            })
            .ToList();
        
        installer.InstallMods(mods, (message, timeTaken) =>
        {
            Console.WriteLine($"{message} installed in {timeTaken}");
        });
        
        totalTime.Stop();
        Console.WriteLine($"Mods installed in {totalTime}");
    }
}
