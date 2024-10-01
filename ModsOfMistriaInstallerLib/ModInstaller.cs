using System.Diagnostics;
using Garethp.ModsOfMistriaInstallerLib.Generator;
using Garethp.ModsOfMistriaInstallerLib.Installer;
using Garethp.ModsOfMistriaInstallerLib.Lang;
using Garethp.ModsOfMistriaInstallerLib.ModTypes;
using Newtonsoft.Json.Linq;

namespace Garethp.ModsOfMistriaInstallerLib;

public class ModInstaller(string fieldsOfMistriaLocation, string modsLocation)
{
    private readonly List<List<string>> _filesToBackup =
    [
        ["__fiddle__.json"],
        ["__mist__.json"],
        ["t2_input.json"],
        ["t2_output.json"],
        ["localization.json"],
        ["animation", "generated", "outlines.json"],
        ["animation", "generated", "player_asset_parts.json"],
        ["animation", "generated", "shadow_manifest.json"],
        ["room_data", "points.json"],
        ["data.win"]
    ];

    public void ValidateMods(List<IMod> mods)
    {
        var desiredGenerators = GetGenerators();
        mods.ForEach(mod =>
        {
            desiredGenerators.ForEach(generator =>
            {
                mod.GetValidation().Merge(generator.Validate(mod));
            });
        });
    }

    public List<string> PreinstallInformation(List<IMod> mods)
    {
        var information = new List<string>();

        var generatedInformation = new GeneratedInformation();

        var desiredGenerators = GetGenerators();
        var desiredInstallers = GetInstallers();
        
        foreach (var mod in mods)
        {
            foreach (var generator in desiredGenerators.Where(generator => generator.CanGenerate(mod)))
            {
                generatedInformation.Merge(generator.Generate(mod));
            }
        }
        
        foreach (var installer in desiredInstallers)
        {
            if (installer is not IPreinstallInfo preinstallChecker) continue;
            
            information.AddRange(preinstallChecker.GetPreinstallInformation(generatedInformation));
        }
        
        return information;
    }
    
    public List<string> PreUninstallInformation()
    {
        var information = new List<string>();
        
        var desiredInstallers = GetInstallers();
        
        foreach (var installer in desiredInstallers)
        {
            if (installer is not IPreUninstallInfo preUninstallInfo) continue;
            
            information.AddRange(preUninstallInfo.GetPreUninstallInformation());
        }
        
        return information;
    }
    
    public void InstallMods(List<IMod> mods, Action<string, string> reportStatus)
    {
        var totalTime = new Stopwatch();
        totalTime.Start();
        if (!Directory.Exists(fieldsOfMistriaLocation))
        {
            throw new DirectoryNotFoundException(Resources.MistriaLocationDoesNotExist);
        }
        
        if (IsFreshInstall())
        {
            _filesToBackup.ForEach(filePath =>
            {
                var path = Path.Combine(new List<string> {fieldsOfMistriaLocation}.Concat(filePath).ToArray());
                if (!File.Exists(path)) return;

                var extension = Path.GetExtension(path);
                var backupPath = path.Replace(extension, ".bak" + extension);

                if (File.Exists(backupPath))
                {
                    File.Delete(backupPath);
                }
            });
        }

        var generatedInformation = new GeneratedInformation();

        var desiredGenerators = GetGenerators();
        var desiredInstallers = GetInstallers();
        
        foreach (var mod in mods)
        {
            reportStatus(string.Format(Resources.GeneratingInformationForMod, mod.GetId()), "");
            foreach (var generator in desiredGenerators.Where(generator => generator.CanGenerate(mod)))
            {
                generatedInformation.Merge(generator.Generate(mod));
            }
        }

        var timer = new Stopwatch();
        
        foreach (var installer in desiredInstallers)
        {
            timer.Restart();
            installer.Install(fieldsOfMistriaLocation, modsLocation, generatedInformation, reportStatus);
            timer.Stop();
            reportStatus(installer.GetType().Name, timer.ToString());
        }
        
        new ChecksumInstaller().Install(fieldsOfMistriaLocation, modsLocation, generatedInformation, reportStatus);
        totalTime.Stop();
        
        reportStatus(Resources.InstallCompleted, totalTime.ToString());
    }

    private bool IsFreshInstall()
    {
        var checksums = JObject.Parse(File.ReadAllText(Path.Combine(fieldsOfMistriaLocation, "checksums.json")));

        return checksums["mods_installed"]?.Value<bool>() != true;
    }

    private List<IGenerator> GetGenerators()
    {
        return (from app in AppDomain.CurrentDomain.GetAssemblies().AsParallel()
            from type in app.GetTypes()
            where type.GetInterface(nameof(IGenerator)) is not null && !type.IsAbstract
            let attributes = type.GetCustomAttributes(typeof(InformationGenerator), true)
            where attributes is { Length: > 0 } && attributes.Any(attribute => (InformationGenerator) attribute is { ManifestVersion: 1 })
            select Activator.CreateInstance(type) as IGenerator).ToList();
    }

    private List<IModuleInstaller> GetInstallers()
    {
        return (from app in AppDomain.CurrentDomain.GetAssemblies().AsParallel()
            from type in app.GetTypes()
            where type.GetInterface(nameof(IModuleInstaller)) is not null && !type.IsAbstract
            let attributes = type.GetCustomAttributes(typeof(InformationInstaller), true)
            where attributes is { Length: > 0 } && attributes.Any(attribute => (InformationInstaller) attribute is { ManifestVersion: 1 })
            select Activator.CreateInstance(type) as IModuleInstaller).ToList();
    }

    public void Uninstall()
    {
        _filesToBackup.ForEach(filePath =>
        {
            var path = Path.Combine(new List<string> {fieldsOfMistriaLocation}.Concat(filePath).ToArray());
            if (!File.Exists(path)) return;

            var extension = Path.GetExtension(path);
            var backupPath = path.Replace(extension, ".bak" + extension);

            if (File.Exists(backupPath))
            {
                File.Delete(path);
                File.Copy(backupPath, path);
                File.Delete(backupPath);
            }
        });
        
        new ChecksumInstaller().Uninstall(fieldsOfMistriaLocation);
        new AurieInstaller().Uninstall();
    }
}