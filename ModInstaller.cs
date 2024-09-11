using Garethp.ModsOfMistriaInstaller.Installer;
using Garethp.ModsOfMistriaInstaller.Installer.Generator;

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
        new SimpleConversationsGenerator()
    ];

    private readonly List<IModuleInstaller> _installers = [
        new AssetPartsInstaller(),
        new ConversationInstaller(),
        new FiddleInstaller(),
        new LocalisationInstaller(),
        new PointsInstaller(),
        new ScheduleInstaller(),
        new ScriptsInstaller(),
    ];
    
    public void InstallMod(string modLocation)
    {
        if (!Directory.Exists(fieldsOfMistriaLocation))
        {
            throw new DirectoryNotFoundException("The Fields of Mistria location does not exist.");
        }
        
        if (!Directory.Exists(modLocation))
        {
            throw new DirectoryNotFoundException("The mod location does not exist.");
        }

        var generatedInformation = new GeneratedInformation();

        foreach (var generator in _generators.Where(generator => generator.CanGenerate(modLocation)))
        {
            generatedInformation.Merge(generator.Generate(modLocation));
        }

        foreach (var installer in _installers)
        {
            installer.Install(fieldsOfMistriaLocation, generatedInformation);
        }
        
        return;
    }
}