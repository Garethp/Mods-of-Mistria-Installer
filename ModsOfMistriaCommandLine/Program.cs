// See https://aka.ms/new-console-template for more information

using CommandLine;
using CommandLine.Text;
using Garethp.ModsOfMistriaCommandLine;
using Garethp.ModsOfMistriaInstallerLib;
using Garethp.ModsOfMistriaInstallerLib.Lang;
using System.Reflection;

class Program
{
    static async Task Main(string[] args)
    {
        Logger.LogAdded += (_, e) => Console.WriteLine(e.Message);

        // Default to the install verb.
        if (args.Length == 0)
        {
            args = ["install"];
        }

        var parser = new Parser(settings =>
        {
            settings.HelpWriter = null;
            settings.AutoHelp = false;
            settings.AutoVersion = false;
        });
        var parserResult = parser.ParseArguments<VersionOptions, HelpOptions, InstallOptions, UninstallOptions, ListOptions>(args);
        await parserResult
            .MapResult(
                (VersionOptions options) => RunVersionAndReturnExitCode(options),
                (HelpOptions options) => RunFullHelpAndReturnExitCode(options,
                    parser.ParseArguments<VersionOptions, HelpOptions, InstallOptions, UninstallOptions, ListOptions>(Array.Empty<string>())),
                (InstallOptions options) => RunInstallAndReturnExitCode(options),
                (UninstallOptions options) => RunUninstallAndReturnExitCode(options),
                (ListOptions options) => RunListAndReturnExitCode(options),
                errors =>
                {
                    var helpText = HelpText.AutoBuild(parserResult, h =>
                    {
                        var currentExe = Assembly.GetEntryAssembly();
                        var title =
                            currentExe!.GetCustomAttribute<AssemblyTitleAttribute>()?.Title ?? "ModsOfMistriaInstaller-cli";
                        var version =
                            currentExe!.GetCustomAttribute<AssemblyFileVersionAttribute>()?.Version ?? "0.1.0";

                        h.Heading = new HeadingInfo(title, version);
                        h.AutoHelp = true;
                        h.AutoVersion = false;
                        return HelpText.DefaultParsingErrorsHandler(parserResult, h);
                    }, e => e);

                    Console.WriteLine(helpText);

                    return Task.FromResult(1);
                }
            );
    }

    static async Task<int> RunVersionAndReturnExitCode(VersionOptions options)
    {
        var currentExe = Assembly.GetEntryAssembly();
        var currentVersionString =
            currentExe!.GetCustomAttribute<AssemblyFileVersionAttribute>()?.Version ?? "0.1.0";

        Console.WriteLine(currentVersionString);

        return 0;
    }

    static async Task<int> RunFullHelpAndReturnExitCode<T>(HelpOptions options, ParserResult<T> parserResult)
    {
        var helpText = HelpText.AutoBuild(parserResult, h =>
        {
            var currentExe = Assembly.GetEntryAssembly();
            var title =
                currentExe!.GetCustomAttribute<AssemblyTitleAttribute>()?.Title ?? "ModsOfMistriaInstaller-cli";
            var version =
                currentExe!.GetCustomAttribute<AssemblyFileVersionAttribute>()?.Version ?? "0.1.0";

            h.Heading = new HeadingInfo(title, version);
            h.AutoHelp = false;
            h.AutoVersion = false;
            return HelpText.DefaultParsingErrorsHandler(parserResult, h);
        }, e => e, verbsIndex: false);

        Console.WriteLine(helpText);

        return 0;
    }

    static async Task<int> RunInstallAndReturnExitCode(InstallOptions options)
    {
        var currentExe = Assembly.GetEntryAssembly();
        var currentVersionString =
            currentExe!.GetCustomAttribute<AssemblyFileVersionAttribute>()?.Version ?? "0.1.0";

        Logger.Log(Resources.CLIRunningBuild, currentVersionString);

        if (Environment.Is64BitOperatingSystem && !Environment.Is64BitProcess)
        {
            Logger.Log(Resources.CLIWarning32Bit);
        }

        Standalone.Run();
        Logger.Log(Resources.CLICompleted);

        if (Environment.GetEnvironmentVariable("EXIT_ON_COMPLETE") != "true")
        {
            Console.ReadKey();
        }

        return 0;
    }

    static async Task<int> RunUninstallAndReturnExitCode(UninstallOptions options)
    {
        var currentExe = Assembly.GetEntryAssembly();
        var currentVersionString =
            currentExe!.GetCustomAttribute<AssemblyFileVersionAttribute>()?.Version ?? "0.1.0";

        Logger.Log(Resources.CLIRunningBuild, currentVersionString);

        if (Environment.Is64BitOperatingSystem && !Environment.Is64BitProcess)
        {
            Logger.Log(Resources.CLIWarning32Bit);
        }

        Standalone.UnInstall();
        Logger.Log(Resources.CLIUninstallComplete);

        if (Environment.GetEnvironmentVariable("EXIT_ON_COMPLETE") != "true")
        {
            Console.ReadKey();
        }

        return 0;
    }

    static async Task<int> RunListAndReturnExitCode(ListOptions options)
    {
        var allMods = Standalone.ListMods();
        if (allMods != null)
        {
            if (allMods.Count == 0)
            {
                Logger.Log(Resources.GUINoModsToInstall);
            }
            else
            {
                var installedMods = allMods.Where(m => m.IsInstalled());
                var notInstalledMods = allMods.Where(m => !m.IsInstalled());

                Logger.Log("Installed mods:");
                foreach (var mod in installedMods)
                {
                    Logger.Log(Resources.GUIModByAuthor, $"- {mod.GetName()} ({mod.GetVersion()})", mod.GetAuthor());
                }

                Logger.Log("Not-installed mods:");
                foreach (var mod in notInstalledMods)
                {
                    Logger.Log(Resources.GUIModByAuthor, $"- {mod.GetName()} ({mod.GetVersion()})", mod.GetAuthor());
                }
            }
        }
        else
        {
            Logger.Log("Failed to load mods.");
        }

        return 0;
    }
}
