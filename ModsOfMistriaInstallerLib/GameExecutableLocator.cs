namespace Garethp.ModsOfMistriaInstallerLib;

/// <summary>
/// Finds the game executable across native Windows/Linux installs, SteamOS
/// Proton installs, and the macOS application bundle layout.
/// </summary>
public static class GameExecutableLocator
{
    public static string? Find(string gameDirectory)
    {
        if (string.IsNullOrWhiteSpace(gameDirectory) || !Directory.Exists(gameDirectory))
            return null;

        var directCandidates = new[]
        {
            Path.Combine(gameDirectory, "FieldsOfMistria.exe"),
            Path.Combine(gameDirectory, "FieldsOfMistria"),
        };

        foreach (var candidate in directCandidates)
            if (File.Exists(candidate)) return candidate;

        if (OperatingSystem.IsMacOS())
        {
            foreach (var app in Directory.EnumerateDirectories(gameDirectory, "*.app"))
            {
                var candidate = Path.Combine(app, "Contents", "MacOS", "FieldsOfMistria");
                if (File.Exists(candidate)) return candidate;
            }
        }

        return null;
    }
}
