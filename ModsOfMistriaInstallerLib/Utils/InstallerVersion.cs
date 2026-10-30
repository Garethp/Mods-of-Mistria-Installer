using System.Reflection;

namespace Garethp.ModsOfMistriaInstallerLib.Utils;

// The running installer's release, read from the entry assembly's file version
// the same way the manifests' minimum-version check reads it. A host without
// one, such as a test runner, gets null rather than a made-up number.
public static class InstallerVersion
{
    public static string? Current()
    {
        var version = Assembly.GetEntryAssembly()
            ?.GetCustomAttribute<AssemblyFileVersionAttribute>()?.Version;
        return string.IsNullOrWhiteSpace(version) ? null : version;
    }
}
