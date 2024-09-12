// See https://aka.ms/new-console-template for more information

using System.Diagnostics;
using Garethp.ModsOfMistriaInstaller;

var totalTime = new Stopwatch();
totalTime.Start();

var modsFolder = "D:\\SteamLibrary\\steamapps\\common\\RimWorld\\Mods\\FoMInstaller\\mods";
var mods = Directory
    .GetDirectories(modsFolder)
    .Where(folder => File.Exists(Path.Combine(folder, "manifest.json")))
    .ToList();

var installer = new ModInstaller("D:\\SteamLibrary\\steamapps\\common\\Fields of Mistria");

installer.InstallMods(mods);

totalTime.Stop();
Console.WriteLine($"Mods installed in {totalTime}");