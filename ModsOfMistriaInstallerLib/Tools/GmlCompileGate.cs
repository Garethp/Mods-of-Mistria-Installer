using System.Diagnostics;
using System.Text;

namespace Garethp.ModsOfMistriaInstallerLib.Tools;

// Runs the bundled `momi-gml-check` over staged GML. The checker is the only
// backend (D13): it compiles every path in one invocation and never executes,
// so it is safe to point at user mod code.
//
// The compat dialect late-binds unknown identifiers, so staged engine files
// reaching game-only names compile headlessly. The gate catches syntax and
// structural errors, same-file duplicate functions and stdlib shadowing, plus
// cross-chunk duplicate exports in unit mode. It does not catch unresolved
// names; no fabricator mode rejects those at compile time.
public class GmlCompileGate : ICompileGate
{
    private static readonly string CheckerExe =
        OperatingSystem.IsWindows() ? "momi-gml-check.exe" : "momi-gml-check";

    private readonly string? _checkerPath;

    public GmlCompileGate(string? checkerPath = null)
    {
        _checkerPath = checkerPath ?? ResolveChecker();
    }

    public bool Available => _checkerPath is not null;

    // The gate for `mode`, or null when it must not run. Auto is on when a
    // backend resolves and skipped with a log line when not; mandatory makes a
    // missing backend an error (CI); off never runs. The caller decides what a
    // failure costs: a shared-set failure aborts the apply, a single mod's
    // failure excludes that mod.
    public static ICompileGate? Resolve(CompileGateMode mode)
    {
        if (mode == CompileGateMode.Off) return null;

        var gate = new GmlCompileGate();
        if (gate.Available) return gate;

        if (mode == CompileGateMode.Mandatory)
            throw new InvalidOperationException(
                "compile gate: no checker backend resolved and the gate is mandatory - build "
                + "tools/checker (cargo build --release --manifest-path tools/checker/Cargo.toml) "
                + "or set MOMI_GML_CHECKER");

        Logger.Log("  compile gate: no checker binary found, skipping the compile pass.");
        return null;
    }

    public void RunFiles(IReadOnlyList<string> paths) => Run("files", paths);

    public void RunUnit(IReadOnlyList<string> paths) => Run("unit", paths);

    // Discovery order: MOMI_GML_CHECKER (set-but-missing fails loudly rather
    // than silently demoting to no gate), else beside the app (where the
    // single-file bundle extracts), else the repo dev build. Null means no
    // backend: an auto gate logs and skips, a mandatory gate errors.
    public static string? ResolveChecker()
    {
        var overridePath = Environment.GetEnvironmentVariable("MOMI_GML_CHECKER");
        if (!string.IsNullOrEmpty(overridePath))
        {
            if (!File.Exists(overridePath))
                throw new FileNotFoundException(
                    $"MOMI_GML_CHECKER points at a missing checker binary: {overridePath} - "
                    + "build tools/checker (cargo build --release --manifest-path "
                    + "tools/checker/Cargo.toml) or unset MOMI_GML_CHECKER", overridePath);
            return overridePath;
        }

        var bundled = Path.Combine(AppContext.BaseDirectory, CheckerExe);
        if (File.Exists(bundled)) return EnsureExecutable(bundled);

        var devBuild = Path.Combine(RepoRoot() ?? "", "tools", "checker", "target", "release", CheckerExe);
        return File.Exists(devBuild) ? devBuild : null;
    }

    private void Run(string mode, IReadOnlyList<string> paths)
    {
        if (_checkerPath is null)
            throw new InvalidOperationException(
                "compile gate: no checker backend resolved - build tools/checker or set "
                + "MOMI_GML_CHECKER");
        if (paths.Count == 0) return;

        var missing = paths.Where(p => !File.Exists(p)).ToList();
        if (missing.Count > 0)
            throw new InvalidOperationException(
                $"compile gate: staged file vanished: {string.Join(", ", missing)}");

        // Paths ride a listfile to stay clear of command-line length limits.
        // UTF-8 with no BOM, one native path per line: the binary reads it
        // lossily, and a BOM would corrupt the first path.
        var listFile = Path.Combine(Path.GetTempPath(), $"momi_check_{Guid.NewGuid():N}.txt");
        string stderr;
        int exitCode;
        try
        {
            File.WriteAllText(listFile, string.Join("\n", paths), new UTF8Encoding(false));

            var startInfo = new ProcessStartInfo(_checkerPath)
            {
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                // A windowed host has no console, so a console-subsystem child
                // flashes an empty window; the streams are already redirected,
                // so suppressing it changes nothing the gate reads
                CreateNoWindow = true,
            };
            startInfo.ArgumentList.Add(mode);
            startInfo.ArgumentList.Add("--files-from");
            startInfo.ArgumentList.Add(listFile);

            using var process = Process.Start(startInfo)
                                ?? throw new InvalidOperationException(
                                    $"compile gate: could not start {_checkerPath}");
            // stdout is discarded; the compiler's diagnostics are on stderr
            process.StandardOutput.ReadToEnd();
            stderr = process.StandardError.ReadToEnd();
            process.WaitForExit();
            exitCode = process.ExitCode;
        }
        finally
        {
            if (File.Exists(listFile)) File.Delete(listFile);
        }

        if (exitCode != 0)
            // the compiler's own message is the diagnostic, so it rides verbatim
            throw new InvalidOperationException(
                $"compile pass FAILED (exit {exitCode}):\n{stderr.Trim()}");

        Logger.Log($"  compile pass: {paths.Count} file(s) OK ({Path.GetFileName(_checkerPath)}).");
    }

    // The single-file bundle extracts the checker beside the app without the
    // execute bit, so set it where that matters.
    private static string EnsureExecutable(string path)
    {
        if (OperatingSystem.IsWindows()) return path;

        var mode = File.GetUnixFileMode(path);
        if (!mode.HasFlag(UnixFileMode.UserExecute))
            File.SetUnixFileMode(path, mode | UnixFileMode.UserExecute
                                            | UnixFileMode.GroupExecute | UnixFileMode.OtherExecute);
        return path;
    }

    // The repo root, walked up from the running binary. Null outside a repo,
    // which is every end-user install.
    private static string? RepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "ModsOfMistriaInstaller.sln")))
            dir = dir.Parent;
        return dir?.FullName;
    }
}
