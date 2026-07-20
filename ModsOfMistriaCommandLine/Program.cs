// See https://aka.ms/new-console-template for more information

using System.Reflection;
using Garethp.ModsOfMistriaInstallerLib;
using Garethp.ModsOfMistriaInstallerLib.GmlMods;
using Garethp.ModsOfMistriaInstallerLib.Lang;
using Garethp.ModsOfMistriaInstallerLib.ModTypes;
using Garethp.ModsOfMistriaInstallerLib.Operations;
using Garethp.ModsOfMistriaInstallerLib.Seam;
using Garethp.ModsOfMistriaInstallerLib.Tools;

var currentExe = Assembly.GetEntryAssembly();
var currentVersionString =
    currentExe!.GetCustomAttribute<AssemblyFileVersionAttribute>()?.Version ?? "0.1.0";

if (args.Contains("--version"))
{
    Console.WriteLine(currentVersionString);
    Environment.Exit(0);
}

// The CLI parses and prints; the behaviour behind each flag lives in the Lib.
// Exit codes: 0 ok, 1 error, 2 usage.
var gateMode = CompileGateMode.Auto;
switch (FlagValue(args, "--compile-check"))
{
    case null or "on":  // "on" is the default made explicit: run when a backend resolves
        break;
    case "off":
        gateMode = CompileGateMode.Off;
        break;
    case "require":
        gateMode = CompileGateMode.Mandatory;
        break;
    default:
        Console.WriteLine(Resources.CLICompileCheckUsage);
        Environment.Exit(2);
        break;
}

// The seam check runs before the console subscribes to Logger, so its report
// (JSON included) is the only thing on stdout.
if (args.Contains("--seam-check") || args.Contains("--seam-check-json"))
{
    Environment.Exit(RunSeamCheck(args));
}

// Lint likewise: the stage's own log lines stay internal and the report is
// the only thing on stdout.
if (args.Contains("--lint"))
{
    Environment.Exit(RunLint(args, gateMode));
}

Logger.LogAdded += (_, e) => Console.WriteLine(e.Message);

Logger.Log(Resources.CLIRunningBuild, currentVersionString);

if (Environment.Is64BitOperatingSystem && !Environment.Is64BitProcess)
{
    Logger.Log(Resources.CLIWarning32Bit);
}

var exitCode = 0;
if (args.Contains("--uninstall"))
{
    try
    {
        Standalone.UnInstall();
        Logger.Log(Resources.CLIUninstallComplete);
    }
    catch (Exception exception)
    {
        Logger.Log(exception.Message);
        exitCode = 1;
    }
}
else
{
    var gmlOptions = new GmlLayerOptions
    {
        StrictLints = args.Contains("--strict-lints"),
        FailOnSkip = args.Contains("--fail-on-skip"),
    };

    try
    {
        Standalone.Run(gmlOptions, gateMode);
        Logger.Log(Resources.CLICompleted);
    }
    catch (Exception exception)
    {
        Logger.Log(exception.Message);
        exitCode = 1;
    }
}

if (Environment.GetEnvironmentVariable("EXIT_ON_COMPLETE") != "true")
{
    Console.ReadKey();
}

Environment.Exit(exitCode);

// The arg after the flag, "" when the flag is last, null when absent
static string? FlagValue(string[] args, string flag)
{
    var index = Array.IndexOf(args, flag);
    if (index < 0) return null;
    return index + 1 < args.Length ? args[index + 1] : "";
}

// --seam-check [zip] / --seam-check-json [zip]: the located install's backup
// when no zip is given. Exit 0 when every anchor holds, 1 when any broke, 2
// when there is nothing to check against.
static int RunSeamCheck(string[] args)
{
    var zipPath = FlagValue(args, "--seam-check");
    if (IsMissing(zipPath)) zipPath = FlagValue(args, "--seam-check-json");

    if (IsMissing(zipPath))
    {
        var mistriaLocation = MistriaLocator.GetMistriaLocation();
        if (mistriaLocation is null)
        {
            Console.WriteLine(Resources.CoreMistriaNotFound);
            return 2;
        }

        try
        {
            zipPath = SeamVerifier.LocateBackup(mistriaLocation);
        }
        catch (FileNotFoundException exception)
        {
            Console.WriteLine(exception.Message);
            return 2;
        }
    }

    VerifyResult result;
    try
    {
        using var pristine = new ZipPristineSource(zipPath!);
        result = SeamVerifier.Verify(pristine);
    }
    catch (FileNotFoundException exception)
    {
        Console.WriteLine(exception.Message);
        return 2;
    }

    Console.WriteLine(args.Contains("--seam-check-json")
        ? SeamVerifier.ToJson(result, zipPath!)
        : SeamVerifier.RenderText(result, zipPath!));
    return result.ExitCode;
}

// --lint <mod folder> [zip]: would the apply install this mod? Runs the
// manifest validation and the full read-only GML staging - skip pass, lints
// and compile gate - against the pristine zip (the located install's backup
// when none is given) and prints the report. --strict-lints and
// --compile-check compose exactly as they do on an install. Exit 0 when the
// mod would install, 1 when it would be skipped, 2 when the lint cannot run.
static int RunLint(string[] args, CompileGateMode gateMode)
{
    var modPath = FlagValue(args, "--lint");
    if (IsMissing(modPath))
    {
        Console.WriteLine(Resources.CLILintUsage);
        return 2;
    }

    // the optional pristine zip rides as a second positional, --seam-check style
    var lintIndex = Array.IndexOf(args, "--lint");
    var zipPath = lintIndex + 2 < args.Length && !args[lintIndex + 2].StartsWith("--")
        ? args[lintIndex + 2]
        : null;

    if (zipPath is null)
    {
        var mistriaLocation = MistriaLocator.GetMistriaLocation();
        if (mistriaLocation is null)
        {
            Console.WriteLine(Resources.CoreMistriaNotFound);
            return 2;
        }

        try
        {
            zipPath = SeamVerifier.LocateBackup(mistriaLocation);
        }
        catch (FileNotFoundException exception)
        {
            Console.WriteLine(exception.Message);
            return 2;
        }
    }

    var location = FolderMod.GetModLocation(modPath!);
    if (location is null)
    {
        Console.WriteLine(Resources.CoreCouldNotFindModManifest);
        return 2;
    }

    try
    {
        var mod = FolderMod.FromManifest(location);
        var options = new GmlLayerOptions { StrictLints = args.Contains("--strict-lints") };
        using var pristine = new ZipPristineSource(zipPath);
        var result = ModLinter.Lint(mod, pristine, GmlCompileGate.Resolve(gateMode), options);
        Console.WriteLine(ModLinter.RenderText(result, location));
        return result.ExitCode;
    }
    catch (Exception exception)
    {
        // an unreadable zip, stale anchors or a malformed manifest: the lint
        // could not run, which says nothing about the mod itself
        Console.WriteLine(exception.Message);
        return 2;
    }
}

static bool IsMissing(string? zipPath) =>
    zipPath is null || zipPath.Length == 0 || zipPath.StartsWith("--");
