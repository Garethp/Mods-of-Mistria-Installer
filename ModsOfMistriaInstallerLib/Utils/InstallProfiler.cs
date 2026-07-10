using System.Diagnostics;

namespace Garethp.ModsOfMistriaInstallerLib.Utils;

/// <summary>
/// Lightweight install-phase profiler. Enable with MOMI_PROFILE=1 or --profile.
/// </summary>
public static class InstallProfiler
{
    private static readonly Dictionary<string, ProfilerEntry> _entries = new(StringComparer.OrdinalIgnoreCase);
    private static bool _enabled;

    public static bool Enabled
    {
        get => _enabled;
        set => _enabled = value;
    }

    public static void ConfigureFromEnvironment()
    {
        var env = Environment.GetEnvironmentVariable("MOMI_PROFILE");
        _enabled = string.Equals(env, "1", StringComparison.OrdinalIgnoreCase)
                   || string.Equals(env, "true", StringComparison.OrdinalIgnoreCase);
    }

    public static IDisposable Measure(string name)
    {
        if (!_enabled) return NoopScope.Instance;
        return new Scope(name);
    }

    public static void AddCount(string name, long count = 1)
    {
        if (!_enabled) return;
        var entry = GetOrCreate(name);
        entry.Count += count;
    }

    public static void Reset()
    {
        _entries.Clear();
    }

    public static IReadOnlyList<ProfilerReportLine> GetReport()
    {
        return _entries
            .Select(kvp => new ProfilerReportLine(
                kvp.Key,
                kvp.Value.Elapsed,
                kvp.Value.Count))
            .OrderByDescending(line => line.Elapsed)
            .ToList();
    }

    public static string FormatReport()
    {
        var lines = GetReport();
        if (lines.Count == 0)
            return "Install profiler: no data collected.";

        var total = lines.Sum(line => line.Elapsed.TotalMilliseconds);
        var builder = new System.Text.StringBuilder();
        builder.AppendLine("=== MOMI Install Profiler ===");
        foreach (var line in lines)
        {
            var pct = total > 0 ? line.Elapsed.TotalMilliseconds / total * 100 : 0;
            builder.AppendLine(
                $"{line.Name,-40} {line.Elapsed,10:mm\\:ss\\.fff}  ({pct,5:F1}%)  count={line.Count}");
        }
        builder.AppendLine($"{"TOTAL",-40} {TimeSpan.FromMilliseconds(total),10:mm\\:ss\\.fff}");
        return builder.ToString();
    }

    private static ProfilerEntry GetOrCreate(string name)
    {
        if (!_entries.TryGetValue(name, out var entry))
        {
            entry = new ProfilerEntry();
            _entries[name] = entry;
        }
        return entry;
    }

    private sealed class ProfilerEntry
    {
        public TimeSpan Elapsed;
        public long Count;
    }

    private sealed class Scope : IDisposable
    {
        private readonly string _name;
        private readonly Stopwatch _timer = Stopwatch.StartNew();

        public Scope(string name) => _name = name;

        public void Dispose()
        {
            _timer.Stop();
            var entry = GetOrCreate(_name);
            entry.Elapsed += _timer.Elapsed;
            entry.Count++;
        }
    }

    private sealed class NoopScope : IDisposable
    {
        public static readonly NoopScope Instance = new();
        public void Dispose() { }
    }

    public readonly record struct ProfilerReportLine(string Name, TimeSpan Elapsed, long Count);
}
