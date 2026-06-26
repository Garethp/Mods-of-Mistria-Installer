using Garethp.ModsOfMistriaInstallerLib.ModTypes;
using Garethp.ModsOfMistriaInstallerLib.Utils;

namespace Garethp.ModsOfMistriaInstallerLib.Installer;

// Stub — JSON installation is not yet implemented.
public class JSONInstaller : Installer
{
    public JSONInstaller(
        string fomLocation,
        InstallManifest manifest,
        Dictionary<string, string> fileNameUIDMapping)
        : base(fomLocation, manifest, fileNameUIDMapping) { }

    public override void Install(IMod mod, Action<string, string> reportStatus)
    {
        // Not implemented yet.
    }
}
