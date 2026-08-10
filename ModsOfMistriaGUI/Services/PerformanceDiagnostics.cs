using System.Diagnostics;
using Garethp.ModsOfMistriaInstallerLib;

namespace Garethp.ModsOfMistriaGUI.Services;

/// <summary>
/// Performance measurements used while diagnosing startup, discovery, and
/// localization refreshes. Calls are compiled out of Release builds.
/// </summary>
internal static class PerformanceDiagnostics
{
    [Conditional("DEBUG")]
    public static void Log(string message) => Logger.Log(message);
}
