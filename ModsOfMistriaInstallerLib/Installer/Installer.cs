using Garethp.ModsOfMistriaInstallerLib.ModTypes;
using Garethp.ModsOfMistriaInstallerLib.Utils;

namespace Garethp.ModsOfMistriaInstallerLib.Installer;

// Abstract base for all file-type-specific installers.
// Provides shared services: path resolution, dirty-tracking, and the shared
// filename→UID mapping that lets related files (spr_ + poly_) share IDs.
public abstract class Installer
{
    protected readonly string FomLocation;
    protected readonly string AssetsLocation;
    protected readonly InstallManifest Manifest;
    protected readonly Dictionary<string, string> FileNameUIDMapping;

    protected Installer(
        string fomLocation,
        InstallManifest manifest,
        Dictionary<string, string> fileNameUIDMapping)
    {
        FomLocation        = fomLocation;
        AssetsLocation     = Path.Combine(fomLocation, "assets");
        Manifest           = manifest;
        FileNameUIDMapping = fileNameUIDMapping;
    }

    public abstract void Install(IMod mod, Action<string, string> reportStatus);

    // Returns the absolute destination path in assets/ for a file that lives at
    // relPath inside the mod.
    protected string DestinationPath(string relPath) =>
        Path.Combine(AssetsLocation, relPath.Replace('/', Path.DirectorySeparatorChar));

    // Ensures directories exist, then marks the destination for tracking before
    // any write occurs.  Call this before writing/copying to destPath.
    protected void Dirty(string destPath)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(destPath)!);

        if (File.Exists(destPath))
            Manifest.TrackModified(destPath);
        else
            Manifest.TrackAdded(destPath);
    }
}
