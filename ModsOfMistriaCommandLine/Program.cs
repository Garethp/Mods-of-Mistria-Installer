// See https://aka.ms/new-console-template for more information

using System.Reflection;
using Garethp.ModsOfMistriaInstallerLib;
using ModsOfMistriaCommandLine.Lang;

var currentExe = Assembly.GetEntryAssembly();
var currentVersionString =
    currentExe!.GetCustomAttribute<AssemblyFileVersionAttribute>()?.Version ?? "0.1.0";

if (args.Contains("--version"))
{
    Console.WriteLine(currentVersionString);
    Environment.Exit(0);
}

Logger.LogAdded += (_, e) => Console.WriteLine(e.Message);

Logger.Log(Resources.RunningBuild, currentVersionString);

if (Environment.Is64BitOperatingSystem && !Environment.Is64BitProcess)
{
    Logger.Log(Resources.Warning32Bit);
}

if (args.Contains("--uninstall"))
{
    Standalone.UnInstall();
    Logger.Log(Resources.UninstallComplete);
}
else
{
    Standalone.Run();
    Logger.Log(Resources.Completed);
}

if (Environment.GetEnvironmentVariable("EXIT_ON_COMPLETE") != "true")
{
    Console.ReadKey();
}
