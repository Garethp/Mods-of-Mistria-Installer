using Garethp.ModsOfMistriaInstallerLib.Lang;
using Garethp.ModsOfMistriaInstallerLib.ModTypes;
using Newtonsoft.Json.Linq;

namespace Garethp.ModsOfMistriaInstallerLib;

public class MistriaLocator
{
    public static string? GetMistriaLocation()
    {
        var steamLocations = GetSteamLocations();
        steamLocations
            .Select(location => Path.Combine(location, "common", "Fields of Mistria"))
            .ToList()
            .ForEach(location =>
            {
                Logger.Log(Resources.CoreLookingForMistriaAt, Path.Combine(location, "data.win"));
            });

        Logger.Log(Directory.GetCurrentDirectory());

        var mistriaLocation = steamLocations
            .Where(Path.Exists)
            .Select(location => Path.Combine(location, "common", "Fields of Mistria"))
            .FirstOrDefault(location => Directory.Exists(location) && File.Exists(Path.Combine(location, "data.win")));
        
        if (mistriaLocation is null)
        {
            var currentDirectory = Directory.GetCurrentDirectory();
            if (File.Exists(Path.Combine(currentDirectory, "data.win")))
            {
                Logger.Log(Resources.CoreMistriaNotFoundFallback);
                return currentDirectory;
            }
            
            return mistriaLocation;
        }

        return Path.GetFullPath(mistriaLocation);
    }
    
    public static string? GetModsLocation(string? mistriaLocation)
    {
        var possibleLocations = new List<string>();
        if (mistriaLocation is not null && File.Exists(Path.Combine(mistriaLocation, "data.win")))
        {
            possibleLocations.Add(Path.Combine(mistriaLocation, "mods"));
            possibleLocations.Add(Path.Combine(mistriaLocation, "Mods"));
        }
        
        possibleLocations.Add(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "mistria-mods"));
        possibleLocations.Add(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Mistria-Mods"));

        return possibleLocations
            .Where(location => Directory.Exists(location))
            .Select(location => Path.GetFullPath(location))
            .FirstOrDefault();
    }

    private static IEnumerable<string> GetSteamLocations()
    {
        var locations = new List<string>
        {
            @"C:\Program Files (x86)\Steam\steamapps\",
            @"C:\Program Files\Steam\steamapps\",
            $"{Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)}/steam/steam/steamapps/",
            $"{Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)}/snap/steam/common/.local/share/Steam/steamapps/",
            $"{Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)}/.local/share/Steam/steamapps/",
            $"{Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)}/Library/Application Support/CrossOver/Bottles/Steam/drive_c/Program Files (x86)/Steam/steamapps"
        };

        var drivePotentialLocations = new List<string>
        {
            "SteamLibrary/steamapps/",
            "Steam/steamapps/",
            "Program Files/Steam/steamapps/",
            "Program Files (x86)/Steam/steamapps/",
            "Program Files/SteamLibrary/steamapps/",
            "Program Files (x86)/SteamLibrary/steamapps/"
        };
        
        DriveInfo.GetDrives().ToList().ForEach(drive =>
        {
            locations.AddRange(drivePotentialLocations.Select(location => Path.GetFullPath($@"{drive.RootDirectory.Name}/{location}")));
        });
        
        return locations
            .ToList();
    }

    public static List<IMod> GetMods(string mistriaLocation, string modsLocation)
    {
        var folderMods = Directory
            .GetDirectories(modsLocation)
            .Where(folder => FolderMod.GetModLocation(folder) is not null)
            .Select(location => FolderMod.FromManifest(Path.Combine(FolderMod.GetModLocation(location)!, "manifest.json")));

        IEnumerable<IMod> zipMods = Directory.GetFiles(modsLocation, "*.zip")
            .Select(ZipMod.FromZipFile)
            .Where(zipMod => zipMod is not null)!;

        IEnumerable<IMod> rarMods = Directory.GetFiles(modsLocation, "*.rar")
            .Select(RarMod.FromRarFile)
            .Where(mod => mod is not null)!;

        var mods = new List<IMod>();
        mods.AddRange(folderMods);
        mods.AddRange(zipMods);
        mods.AddRange(rarMods);

        mods = mods.OrderBy(mod => mod.GetName()).ToList();
        
        try
        {
            var installedMods = new List<string>();

            var checksums = JObject.Parse(File.ReadAllText(Path.Combine(mistriaLocation, "checksums.json")));
            if (checksums["mods"] is null) return mods;
            
            foreach (var mod in checksums["mods"]!)
            {
                if (mod["id"] is null) continue;
                installedMods.Add(mod["id"]!.Value<string>());
            }

            foreach (var mod in mods.Where(mod => installedMods.Contains(mod.GetId())))
            {
                mod.SetInstalled(true);
            }
        }
        catch (Exception e)
        {
            // Ignored
        }

        return mods;
    }
}