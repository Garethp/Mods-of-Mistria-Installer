using Garethp.ModsOfMistriaInstallerLib.ModTypes;

namespace Garethp.ModsOfMistriaInstallerLib.Installer;

// Abstract base for all file-type-specific installers.
// Provides shared services: path resolution, dirty-tracking, and the shared
// filename→UID mapping that lets related files (spr_ + poly_) share IDs.
public abstract class Installer
{
    protected readonly Dictionary<string, string> FileNameUIDMapping;

    protected Installer(Dictionary<string, string> fileNameUIDMapping)
    {
        FileNameUIDMapping = fileNameUIDMapping;
    }

    public abstract void Install(IMod mod, Action<string, string> reportStatus);

    // Returns the absolute destination path in assets/ for a file that lives at
    // relPath inside the mod.
    protected string DestinationPath(string relPath) =>
        Path.Combine("assets", relPath.Replace('/', Path.DirectorySeparatorChar));
}
