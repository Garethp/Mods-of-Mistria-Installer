using System.Diagnostics;
using System.Text;

namespace ModsOfMistriaInstallerLibTests.TestUtils;

// Runs GML on the pinned fabricator VM through momi-gml-probe, the shared
// backend for the mmapi runtime suites and the engine-claim probes.
//
// momi-gml-check deliberately cannot do this: it compiles and never executes,
// which is a safety property of the binary the installer invokes on user mod
// code. That is why the probe is a separate binary the release never bundles.
public static class GmlProbe
{
    public record ProbeResult(int ExitCode, string Stdout, string Stderr);

    private static readonly string ProbeExe =
        OperatingSystem.IsWindows() ? "momi-gml-probe.exe" : "momi-gml-probe";

    private static readonly string BuildHint =
        "needs the momi-gml-probe binary (cargo build --release --manifest-path "
        + "tools/checker/Cargo.toml), or set MOMI_GML_PROBE";

    // MOMI_GML_PROBE wins and is returned even when absent, so a bad override
    // fails loudly rather than silently demoting the suite to a skip. Null
    // means no binary: the suites skip locally, and CI always arms them.
    public static string? ProbeBinary()
    {
        var overridePath = Environment.GetEnvironmentVariable("MOMI_GML_PROBE");
        if (!string.IsNullOrEmpty(overridePath))
        {
            if (!File.Exists(overridePath))
                throw new FileNotFoundException(
                    $"MOMI_GML_PROBE points at a missing probe binary: {overridePath}", overridePath);
            return overridePath;
        }

        var devBuild = Path.Combine(RepoRoot() ?? "", "tools", "checker", "target", "release", ProbeExe);
        return File.Exists(devBuild) ? devBuild : null;
    }

    // The resolved probe, or an Assert.Ignore carrying the build hint.
    public static string RequireProbe() =>
        ProbeBinary() ?? throw new IgnoreException(BuildHint);

    public static string Backend() => ProbeBinary() ?? "(no backend)";

    // Compile and execute one .gml file on the pinned VM. stdout is the
    // script's own output (show_debug_message); diagnostics land on stderr.
    public static ProbeResult RunGml(string path)
    {
        var probe = RequireProbe();
        var startInfo = new ProcessStartInfo(probe)
        {
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        };
        startInfo.ArgumentList.Add("run");
        startInfo.ArgumentList.Add(path);

        using var process = Process.Start(startInfo)
                            ?? throw new InvalidOperationException($"could not start {probe}");
        var stdout = process.StandardOutput.ReadToEnd();
        var stderr = process.StandardError.ReadToEnd();
        process.WaitForExit();
        return new ProbeResult(process.ExitCode, stdout, stderr);
    }

    // Run the whole mmapi framework plus `chunks` (in order) as one script.
    // The prelude is not added implicitly: a caller lists exactly the chunks it
    // wants, because the order matters and some suites need a stub layer ahead
    // of the prelude.
    public static ProbeResult RunWithFramework(IReadOnlyList<string> chunks)
    {
        var source = new StringBuilder(FrameworkSource());
        foreach (var chunk in chunks)
        {
            source.Append('\n');
            source.Append(File.ReadAllText(chunk));
        }

        // .gml is load-bearing: it selects the compat dialect
        var script = Path.Combine(Path.GetTempPath(), $"momi_probe_{Guid.NewGuid():N}.gml");
        try
        {
            File.WriteAllText(script, source.ToString(), new UTF8Encoding(false));
            return RunGml(script);
        }
        finally
        {
            if (File.Exists(script)) File.Delete(script);
        }
    }

    // Every carried mmapi .gml, ordinal-sorted, as one chunk. All of them, not
    // just the module under test: the boot compiles the framework as a single
    // unit, so this matches what actually runs. The extra files reach game-only
    // names, which is harmless - the compat dialect late-binds, so a name that
    // is never called never resolves.
    public static string FrameworkSource()
    {
        var dir = Path.Combine(AppContext.BaseDirectory, "Payload", "mmapi");
        var files = Directory.GetFiles(dir, "*.gml").Order(StringComparer.Ordinal).ToList();
        if (files.Count == 0) throw new InvalidOperationException($"no mmapi framework .gml found in {dir}");
        return string.Join("\n", files.Select(File.ReadAllText));
    }

    // PASS/FAIL is the wire format. Exit 0, at least one PASS and no FAIL: the
    // PASS floor is not ceremony, since a body that asserts nothing would
    // otherwise pass vacuously, which is the silence these suites exist to break.
    public static void AssertAllPass(ProbeResult result, string label)
    {
        Assert.That(result.ExitCode, Is.EqualTo(0),
            $"{label} did not run to completion under {Backend()}.\n"
            + "A non-zero exit here usually means an exception escaped a dispatcher - "
            + "which is itself the failure.\n"
            + $"--- stdout ---\n{result.Stdout}\n--- stderr ---\n{result.Stderr}");

        var lines = result.Stdout.Split('\n').Select(l => l.TrimEnd('\r')).ToList();
        var passes = lines.Where(l => l.StartsWith("PASS", StringComparison.Ordinal)).ToList();
        var fails = lines.Where(l => l.StartsWith("FAIL", StringComparison.Ordinal)).ToList();

        Assert.That(passes, Is.Not.Empty, $"{label} asserted nothing:\n{result.Stdout}");
        Assert.That(fails, Is.Empty, $"\n{label} reported FAIL:\n" + string.Join("\n", fails));
    }

    public static string Fixture(string relativePath) =>
        Utils.FixtureHandler.GetFixturePath(Path.Combine("GmlRuntime", relativePath));

    private static string? RepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "ModsOfMistriaInstaller.sln")))
            dir = dir.Parent;
        return dir?.FullName;
    }
}
