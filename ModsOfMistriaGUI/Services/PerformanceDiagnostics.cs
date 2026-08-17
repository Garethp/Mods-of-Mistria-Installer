using System.Diagnostics;
using Garethp.ModsOfMistriaInstallerLib;

namespace Garethp.ModsOfMistriaGUI.Services;

/// <summary>
/// Performance measurements used while diagnosing startup, discovery,
/// localization refreshes, and archive operations.
///
/// Diagnostics are disabled by default. They can be enabled for a local test
/// run with AIM_DIAGNOSTICS=1, or automatically while a debugger is attached.
/// </summary>
internal static class PerformanceDiagnostics
{
    public static readonly bool Enabled =
        Debugger.IsAttached || IsTruthy(Environment.GetEnvironmentVariable("AIM_DIAGNOSTICS"));

    public static readonly bool SuppressInstallProgressUi =
        IsTruthy(Environment.GetEnvironmentVariable("AIM_DIAGNOSTICS_NO_PROGRESS_UI"));

    public static void Log(string message)
    {
        if (Enabled)
            Logger.Log($"[diagnostic] {message}");
    }

    public static string ProcessMetrics()
    {
        if (!Enabled) return "diagnostics=off";
        using var process = Process.GetCurrentProcess();
        return $"cpu={process.TotalProcessorTime.TotalMilliseconds:0} ms, " +
               $"rss={process.WorkingSet64 / 1024d / 1024d:0} MB, " +
               $"threads={process.Threads.Count}";
    }

    private static bool IsTruthy(string? value) =>
        value is not null && (value.Equals("1", StringComparison.OrdinalIgnoreCase)
                              || value.Equals("true", StringComparison.OrdinalIgnoreCase)
                              || value.Equals("on", StringComparison.OrdinalIgnoreCase));
}
