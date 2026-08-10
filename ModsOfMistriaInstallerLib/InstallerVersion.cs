namespace Garethp.ModsOfMistriaInstallerLib;

/// <summary>
/// Version values used when validating existing MOMI-format mods.
/// AIM has its own public release line, but remains compatible with the
/// latest upstream MOMI 0.15.x manifest requirements.
/// </summary>
public static class InstallerVersion
{
    public static readonly Version ModCompatibilityVersion = new(0, 15, 7);
}
