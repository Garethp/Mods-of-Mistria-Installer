// See https://aka.ms/new-console-template for more information

using System.Diagnostics;
using Garethp.ModsOfMistriaInstaller;

var modOverride = "D:\\SteamLibrary\\steamapps\\common\\RimWorld\\Mods\\FoMInstaller\\mods";

var detectedLocation = MistriaLocator.GetMistriaLocation();
if (detectedLocation == null)
{
    Console.WriteLine("Could not find Fields of Mistria location.");
    return;
}

var detectedModsLocation = modOverride;

if (modOverride is null || !Directory.Exists(modOverride))
{
    detectedModsLocation =
        Path.Combine(detectedLocation, "mods");
}

if (!Directory.Exists(detectedModsLocation))
{
    detectedModsLocation = Path.Combine(detectedLocation, "Mods");
}

if (!Directory.Exists(detectedModsLocation))
{
    Console.WriteLine($"Could not find a mods folder at {Path.Combine(detectedLocation, "mods")}.");
    return;
}

Console.WriteLine($"Guessed Location: {detectedLocation}");

var totalTime = new Stopwatch();
totalTime.Start();

var mods = Directory
    .GetDirectories(detectedModsLocation)
    .Where(folder => File.Exists(Path.Combine(folder, "manifest.json")))
    .Select(location => Mod.FromManifest(Path.Combine(location, "manifest.json")))
    .ToList();

var installer = new ModInstaller(detectedLocation);

installer.InstallMods(mods);

totalTime.Stop();
Console.WriteLine($"Mods installed in {totalTime}");