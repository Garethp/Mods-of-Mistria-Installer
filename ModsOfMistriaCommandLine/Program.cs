// See https://aka.ms/new-console-template for more information

using System.Reflection;
using Garethp.ModsOfMistriaInstallerLib;
using Garethp.ModsOfMistriaInstallerLib.Lang;
using Garethp.ModsOfMistriaInstallerLib.Utils;

var currentExe = Assembly.GetEntryAssembly();
var currentVersionString =
    currentExe!.GetCustomAttribute<AssemblyFileVersionAttribute>()?.Version ?? "0.1.0";

if (args.Contains("--version"))
{
    Console.WriteLine(currentVersionString);
    Environment.Exit(0);
}

Logger.LogAdded += (_, e) => Console.WriteLine(e.Message);

Logger.Log(Resources.CLIRunningBuild, currentVersionString);

if (Environment.Is64BitOperatingSystem && !Environment.Is64BitProcess)
{
    Logger.Log(Resources.CLIWarning32Bit);
}

var profile = args.Contains("--profile");
if (profile)
{
    InstallProfiler.Enabled = true;
    Environment.SetEnvironmentVariable("MOMI_PROFILE", "1");
}

var gamePath = GetArgValue(args, "--game-path");
var modsPath = GetArgValue(args, "--mods-path");
var modFilter = GetArgValue(args, "--mod");
var generateCount = GetArgInt(args, "--generate-benchmark");

if (generateCount > 0)
{
    if (string.IsNullOrWhiteSpace(gamePath) || string.IsNullOrWhiteSpace(modsPath))
    {
        Console.Error.WriteLine("--generate-benchmark requires --game-path and --mods-path");
        Environment.Exit(1);
    }

    var modDir = BenchmarkModGenerator.Generate(gamePath!, modsPath!, generateCount);
    Logger.Log($"Generated benchmark mod at {modDir}");
    Environment.Exit(0);
}

if (args.Contains("--uninstall"))
{
    Standalone.UnInstall(gamePath);
    Logger.Log(Resources.CLIUninstallComplete);
}
else
{
    Standalone.Run(gamePath, modsPath, modFilter);
    Logger.Log(Resources.CLICompleted);
}

if (Environment.GetEnvironmentVariable("EXIT_ON_COMPLETE") != "true")
{
    Console.ReadKey();
}

static string? GetArgValue(string[] args, string name)
{
    for (var i = 0; i < args.Length - 1; i++)
    {
        if (string.Equals(args[i], name, StringComparison.OrdinalIgnoreCase))
            return args[i + 1];
    }

    return null;
}

static int GetArgInt(string[] args, string name)
{
    var value = GetArgValue(args, name);
    return int.TryParse(value, out var parsed) ? parsed : 0;
}
