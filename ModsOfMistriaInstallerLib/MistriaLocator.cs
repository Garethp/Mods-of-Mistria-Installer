using System.Runtime.InteropServices;
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
                Logger.Log(Resources.CoreLookingForMistriaAt, Path.Combine(location, "Maybe.toml"));
            });
        
        Logger.Log(Directory.GetCurrentDirectory());

        var mistriaLocation = steamLocations
            .Where(Path.Exists)
            .Select(location => Path.Combine(location, "common", "Fields of Mistria"))
            .FirstOrDefault(location => Directory.Exists(location) && File.Exists(Path.Combine(location, "Maybe.toml")));
        
        if (mistriaLocation is null)
        {
            var currentDirectory = Directory.GetCurrentDirectory();
            if (File.Exists(Path.Combine(currentDirectory, "Maybe.toml")))
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
        if (mistriaLocation is not null && File.Exists(Path.Combine(mistriaLocation, "Maybe.toml")))
        {
            possibleLocations.Add(Path.Combine(mistriaLocation, "mods"));
            possibleLocations.Add(Path.Combine(mistriaLocation, "Mods"));
        }
        
        possibleLocations.Add(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "mistria-mods"));
        possibleLocations.Add(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Mistria-Mods"));

        return possibleLocations
            .Where(location => Directory.Exists(location))
            .Where(location => !RuntimeInformation.IsOSPlatform(OSPlatform.Linux) || !new FileInfo(location).Attributes.HasFlag(FileAttributes.ReparsePoint))
            .Select(location => Path.GetFullPath(location))
            .FirstOrDefault();
    }

    /// <summary>
    /// Returns every FieldsOfMistria AppData directory that contains a saves folder
    /// (e.g. %LOCALAPPDATA%\FieldsOfMistria\beta\). Falls back to the root directory
    /// if no branch subdirectory is found.
    /// </summary>
    public static IEnumerable<string> GetGameConfigDirectories()
    {
        // The MOMI_GAME_CONFIG_DIR override when set, otherwise the AppData scan.
        // Tests point it at a temp dir to keep writes off a real install.
        var overrideDir = Environment.GetEnvironmentVariable("MOMI_GAME_CONFIG_DIR");
        if (!string.IsNullOrEmpty(overrideDir))
        {
            yield return overrideDir;
            yield break;
        }

        var root = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "FieldsOfMistria");

        if (!Directory.Exists(root)) yield break;

        var withSaves = Directory.GetDirectories(root)
            .Where(d => Directory.Exists(Path.Combine(d, "saves")))
            .ToList();

        if (withSaves.Count > 0)
        {
            foreach (var d in withSaves) yield return d;
        }
        else if (Directory.Exists(Path.Combine(root, "saves")))
        {
            yield return root;
        }
    }

    public static string? GetWineLocation()
    {
        var steamLocations = GetSteamLocations().Where(Directory.Exists).ToList();

        List<string> protonLocations = [];
        
        steamLocations.ForEach(location =>
        {
            var common = Path.Combine(location, "common");
            if (!Directory.Exists(common)) return;

            var children = Directory
                .GetDirectories(common)
                .Where(Directory.Exists)
                .Where(it => Path.GetFileName(it).Contains("Proton"))
                .Where(it => File.Exists(Path.Combine(it, "files/bin/wine64")))
                .Select(it => Path.Combine(it, "files/bin/wine64"))
                .ToList();

            protonLocations.AddRange(children);
        });

        return protonLocations.FirstOrDefault(); 
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
            $"{Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)}/.steam/debian-installation/steamapps/",
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
        IEnumerable<IMod> folderMods = Directory
            .GetDirectories(modsLocation)
            .Where(folder => FolderMod.GetModLocation(folder) is not null)
            .Select(location =>
            {
                try
                {
                    return FolderMod.FromManifest(Path.Combine(FolderMod.GetModLocation(location)!));
                }
                catch (Exception e)
                {
                    Logger.Log(e.Message);
                    return null;
                }
            })
            .Where(mod => mod is not null)!;

        IEnumerable<IMod> zipMods = Directory.GetFiles(modsLocation, "*.zip")
            .Select(manifestFile =>
            {
                try
                {
                    return ZipMod.FromZipFile(manifestFile);
                }
                catch (Exception e)
                {
                    Logger.Log(e.Message);
                    return null;
                }
            })
            .Where(zipMod => zipMod is not null)!;

        IEnumerable<IMod> rarMods = Directory.GetFiles(modsLocation, "*.rar")
            .Select(manifestFile =>
            {
                try
                {
                    return RarMod.FromRarFile(manifestFile);
                }
                catch (Exception e)
                {
                    Logger.Log(e.Message);
                    return null;
                }
            })
            .Where(mod => mod is not null)!;

        var mods = new List<IMod>();
        mods.AddRange(folderMods);
        mods.AddRange(zipMods);
        mods.AddRange(rarMods);

        mods = mods.OrderBy(mod => mod.GetName()).ToList();
        
        mods.ForEach(mod => mod.Validate());
        
        try
        {
            var installedMods = new List<string>();

            // I might just be dum but I don't think we have a checksums.json anymore.
        }
        catch (Exception e)
        {
            // Ignored
        }

        return mods;
    }
}