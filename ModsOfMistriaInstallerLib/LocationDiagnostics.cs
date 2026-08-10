using System.IO.Compression;
using Garethp.ModsOfMistriaInstallerLib.Lang;

namespace Garethp.ModsOfMistriaInstallerLib;

/// <summary>
/// Explains why a candidate game or mods directory is not usable. Discovery
/// intentionally returns null when a candidate is invalid; these messages are
/// for the GUI and CLI surfaces that need to tell the user what to fix.
/// </summary>
public static class LocationDiagnostics
{
    public static string DescribeGame(string? location)
    {
        if (string.IsNullOrWhiteSpace(location))
            return "Fields of Mistria was not detected. Select the game folder containing Maybe.toml.";

        if (!Directory.Exists(location))
            return $"The selected Fields of Mistria folder does not exist: {location}";

        if (!File.Exists(Path.Combine(location, "Maybe.toml")))
            return "The selected folder is not a Fields of Mistria installation because Maybe.toml is missing.";

        var archivePath = Path.Combine(location, "assets.zip");
        if (!File.Exists(archivePath) && !Directory.Exists(Path.Combine(location, "assets")))
            return "The Fields of Mistria installation is missing assets.zip (or its unpacked assets folder). Verify the game files through Steam.";

        if (File.Exists(archivePath))
        {
            try
            {
                using var archive = ZipFile.OpenRead(archivePath);
                if (!archive.Entries.Any(entry => entry.FullName.Replace('\\', '/').StartsWith("assets/", StringComparison.OrdinalIgnoreCase)))
                    return "assets.zip is readable but does not contain the expected assets/ game entries. Verify the game files through Steam.";
            }
            catch (InvalidDataException)
            {
                return "assets.zip is damaged or is not a valid ZIP archive. Verify the game files through Steam.";
            }
            catch (IOException)
            {
                return "assets.zip could not be read. Close the game and verify the game files through Steam.";
            }
        }

        return Resources.ResourceManager.GetString("GUILocationGameDetected", Resources.Culture)
            ?? "Fields of Mistria installation detected.";
    }

    public static string DescribeMods(string? gameLocation, string? modsLocation)
    {
        if (string.IsNullOrWhiteSpace(gameLocation) || !Directory.Exists(gameLocation))
            return "Select a valid Fields of Mistria installation before choosing a mods folder.";

        if (string.IsNullOrWhiteSpace(modsLocation))
            return Resources.ResourceManager.GetString("GUILocationNoModsFolder", Resources.Culture)
                ?? "No mods folder was detected. Select one or create it automatically.";

        if (!Directory.Exists(modsLocation))
            return $"The selected mods folder does not exist: {modsLocation}";

        return Resources.ResourceManager.GetString("GUILocationModsDetected", Resources.Culture)
            ?? "Mods folder detected.";
    }
}
