// See https://aka.ms/new-console-template for more information

using System.Diagnostics;
using Garethp.ModsOfMistriaInstaller;

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

        var mods = Directory
            .GetDirectories(modsLocation)
            .Where(folder => File.Exists(Path.Combine(folder, "manifest.json")))
            .Select(location => Mod.FromManifest(Path.Combine(location, "manifest.json")))
            .Where(mod => mod.CanInstall() == null)
            .ToList();

        var installer = new ModInstaller(mistriaLocation);

        installer.InstallMods(mods, (message, timeTaken) =>
        {
            Console.WriteLine($"{message} installed in {timeTaken}");
        });

        totalTime.Stop();
        Console.WriteLine($"Mods installed in {totalTime}");
    }
}
