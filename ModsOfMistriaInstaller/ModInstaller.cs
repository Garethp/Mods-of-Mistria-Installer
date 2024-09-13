using System.Diagnostics;
using Garethp.ModsOfMistriaInstaller.Installer;
using Garethp.ModsOfMistriaInstaller.Installer.Generator;
using Newtonsoft.Json.Linq;

namespace Garethp.ModsOfMistriaInstaller;

public class ModInstaller(string fieldsOfMistriaLocation)
{
    private readonly List<IGenerator> _generators =
    [
        new ConversationGenerator(),
        new FiddleGenerator(), 
        new LocalisationGenerator(),
        new MistGenerator(),
        new PointsGenerator(),
        new ScheduleGenerator(),
        new SimpleConversationsGenerator(),
        new OutfitGenerator()
    ];

    private readonly List<IModuleInstaller> _installers = [
        new AssetPartsInstaller(),
        new ConversationInstaller(),
        new FiddleInstaller(),
        new LocalisationInstaller(),
        new OutlineInstaller(),
        new PointsInstaller(),
        new ScheduleInstaller(),
        new ScriptsInstaller(),
        new GraphicsInstaller(),
    ];
    
    public void InstallMods(List<Mod> mods, Action<string, string> reportStatus)
    {
        var totalTime = new Stopwatch();
        totalTime.Start();
        if (!Directory.Exists(fieldsOfMistriaLocation))
        {
            throw new DirectoryNotFoundException("The Fields of Mistria location does not exist.");
        }

        if (IsFreshInstall())
        {
            DeleteIfExists([fieldsOfMistriaLocation, "__fiddle__.bak.json"]);
            DeleteIfExists([fieldsOfMistriaLocation, "__mist__.bak.json"]);
            DeleteIfExists([fieldsOfMistriaLocation, "t2_input.bak.json"]);
            DeleteIfExists([fieldsOfMistriaLocation, "t2_output.bak.json"]);
            DeleteIfExists([fieldsOfMistriaLocation, "localization.bak.json"]);
            DeleteIfExists([fieldsOfMistriaLocation, "animation", "generated", "outlines.bak.json"]);
            DeleteIfExists([fieldsOfMistriaLocation, "animation", "generated", "player_asset_parts.bak.json"]);
            DeleteIfExists([fieldsOfMistriaLocation, "room_data", "points.bak.json"]);
        }

        var generatedInformation = new GeneratedInformation();
        
        foreach (var mod in mods)
        {
            if (!Directory.Exists(mod.Location))
            {
                throw new DirectoryNotFoundException("The mod location does not exist.");
            }
            
            reportStatus("Generating information for " + mod.Id, "");
            foreach (var generator in _generators.Where(generator => generator.CanGenerate(mod)))
            {
                generatedInformation.Merge(generator.Generate(mod));
            }
        }

        var timer = new Stopwatch();
        
        foreach (var installer in _installers)
        {
            timer.Restart();
            installer.Install(fieldsOfMistriaLocation, generatedInformation, reportStatus);
            timer.Stop();
            reportStatus(installer.GetType().Name, timer.ToString());
        }
        
        new ChecksumInstaller().Install(fieldsOfMistriaLocation, generatedInformation, reportStatus);
        totalTime.Stop();
        
        reportStatus("Finished", totalTime.ToString());
    }

    bool IsFreshInstall()
    {
        var checksums = JObject.Parse(File.ReadAllText(Path.Combine(fieldsOfMistriaLocation, "checksums.json")));

        return checksums["mods_installed"]?.Value<bool>() != true;
    }
    
    void DeleteIfExists(string[] paths)
    {
        if (File.Exists(Path.Combine(paths)))
        {
            File.Delete(Path.Combine(paths));
        }
    }
}