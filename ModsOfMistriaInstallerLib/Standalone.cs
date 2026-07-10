// See https://aka.ms/new-console-template for more information

using System.Diagnostics;
using Garethp.ModsOfMistriaInstallerLib.Generator;
using Garethp.ModsOfMistriaInstallerLib.Lang;
using Garethp.ModsOfMistriaInstallerLib.Utils;

namespace Garethp.ModsOfMistriaInstallerLib;

public static class Standalone
{
    public static void Run(string? gamePath = null, string? modsPath = null, string? modFilter = null)
    {
        InstallProfiler.ConfigureFromEnvironment();
        InstallProfiler.Reset();

        var mistriaLocation = ResolveGamePath(gamePath);
        if (mistriaLocation == null)
        {
            Logger.Log(Resources.CoreMistriaNotFound);
            return;
        }
        
        var modsLocation = ResolveModsPath(mistriaLocation, modsPath);

        if (modsLocation is null || !Directory.Exists(modsLocation))
        {
            Logger.Log(Resources.CoreCouldNotGuessModsAt, Path.Combine(mistriaLocation, "mods"));
            return;
        }

        Logger.Log(Resources.CoreGuessedMistriaAt, mistriaLocation);

        var totalTime = new Stopwatch();
        totalTime.Start();
        
        var installer = new ModInstaller(mistriaLocation, modsLocation);

        var allMods = MistriaLocator.GetMods(mistriaLocation, modsLocation);
        if (!string.IsNullOrWhiteSpace(modFilter))
        {
            allMods = allMods
                .Where(mod => string.Equals(mod.GetName(), modFilter, StringComparison.OrdinalIgnoreCase)
                              || string.Equals(mod.GetId(), modFilter, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        allMods = allMods
            .Where(mod =>
            {
                if (mod.GetValidation().Status == ValidationStatus.Invalid)
                {
                    Logger.Log(Resources.CoreSkippingModBecauseErrors, mod.GetId());
                    foreach (var error in mod.GetValidation().Errors)
                    {
                        Logger.Log($"  {error.Message}");
                    }
                    
                    return false;
                }

                if (mod.GetValidation().Status == ValidationStatus.Warning)
                {
                    Logger.Log(Resources.CoreModHasWarnings, mod.GetId());
                    foreach (var warning in mod.GetValidation().Warnings)
                    {
                        Logger.Log($"  {warning.Message}");
                    }
                }
                
                return true;
            })
            .ToList();
        
        installer.InstallMods(allMods, (message, timeTaken) =>
        {
            Logger.Log(Resources.CoreInstalledInReporter, message, timeTaken);
        });
        
        totalTime.Stop();
        Logger.Log(Resources.CoreModsInstalledInTime, totalTime);

        if (InstallProfiler.Enabled)
        {
            Logger.Log(InstallProfiler.FormatReport());
        }
    }

    public static void UnInstall(string? gamePath = null)
    {
        var mistriaLocation = ResolveGamePath(gamePath);
        if (mistriaLocation == null)
        {
            Logger.Log(Resources.CoreMistriaNotFound);
            return;
        }
        
        var installer = new ModInstaller(mistriaLocation, "");
        installer.Uninstall();
    }

    private static string? ResolveGamePath(string? gamePath)
    {
        if (!string.IsNullOrWhiteSpace(gamePath) && Directory.Exists(gamePath))
            return Path.GetFullPath(gamePath);

        return MistriaLocator.GetMistriaLocation();
    }

    private static string? ResolveModsPath(string mistriaLocation, string? modsPath)
    {
        if (!string.IsNullOrWhiteSpace(modsPath) && Directory.Exists(modsPath))
            return Path.GetFullPath(modsPath);

        return MistriaLocator.GetModsLocation(mistriaLocation);
    }
}
