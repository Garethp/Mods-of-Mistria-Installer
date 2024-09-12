namespace Garethp.ModsOfMistriaInstaller;

public class MistriaLocator
{
    public static string? GetMistriaLocation()
    {
        var steamLocations = GetSteamLocations();
        var mistriaLocation = steamLocations
            .Select(location => Path.Combine(location, "common", "Fields of Mistria"))
            .FirstOrDefault(location => Directory.Exists(location) && File.Exists(Path.Combine(location, "data.win")));

        return mistriaLocation;
    }
    
    private static IEnumerable<string> GetSteamLocations()
    {
        var locations = new List<string>
        {
            @"C:\Program Files (x86)\Steam\steamapps\",
            @"C:\Program Files\Steam\steamapps\",
            $"{Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)}/steam/steam/steamapps/"
        };

        locations.AddRange(
            DriveInfo
                .GetDrives()
                .Select(drive => $@"{drive.RootDirectory.Name}/SteamLibrary/steamapps/")
        );

        return locations
            .Where(Path.Exists)
            .ToList();
    }
}