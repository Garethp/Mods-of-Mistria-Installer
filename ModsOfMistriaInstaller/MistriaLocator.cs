namespace Garethp.ModsOfMistriaInstaller;

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
                Console.WriteLine($"Looking for Mistria at {Path.GetFullPath(Path.Combine(location, "data.win"))}");
            });

        Console.WriteLine(Directory.GetCurrentDirectory());

        var mistriaLocation = steamLocations
            .Where(Path.Exists)
            .Select(location => Path.Combine(location, "common", "Fields of Mistria"))
            .FirstOrDefault(location => Directory.Exists(location) && File.Exists(Path.Combine(location, "data.win")));
        
        if (mistriaLocation is null)
        {
            var currentDirectory = Directory.GetCurrentDirectory();
            if (File.Exists(Path.Combine(currentDirectory, "data.win")))
            {
                Console.WriteLine("Fields of Mistria not found, falling back to current folder");
                return currentDirectory;
            }
            
            return mistriaLocation;
        }

        return Path.GetFullPath(mistriaLocation);
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
        };

        var drivePotentialLocations = new List<string>()
        {
            "SteamLibrary/steamapps/",
            "Steam/steamapps/",
            "Program Files/Steam/steamapps/",
            "Program Files (x86)/Steam/steamapps/",
            "Program Files/SteamLibrary/steamapps/",
            "Program Files (x86)/SteamLibrary/steamapps/",
        };
        
        DriveInfo.GetDrives().ToList().ForEach(drive =>
        {
            locations.AddRange(drivePotentialLocations.Select(location => Path.GetFullPath($@"{drive.RootDirectory.Name}/{location}")));
        });
        
        return locations
            .ToList();
    }
}