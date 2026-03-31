using System.Diagnostics;
using Garethp.ModsOfMistriaInstallerLib;
using Garethp.ModsOfMistriaInstallerLib.Generator;
using Garethp.ModsOfMistriaInstallerLib.Installer;
using Garethp.ModsOfMistriaInstallerLib.Lang;
using Garethp.ModsOfMistriaInstallerLib.ModTypes;
using Garethp.ModsOfMistriaInstallerLib.Utils;

namespace ModsOfMistriaInstallerLibTests.Utils;

public class MockInstaller
{
    private List<IGenerator> _generators;
    private List<IModuleInstaller> _installers;

    public MockInstaller(List<IGenerator> generators, List<IModuleInstaller> installers)
    {
        _generators = generators;
        _installers = installers;
    }

    public GeneratedInformation InstallMods(List<IMod> mods, IFileModifier fileModifier)
    {
        var generatedInformation = new List<GeneratedInformationWithMod>();

        var desiredGenerators = _generators;
        var desiredInstallers = _installers;

        foreach (var mod in mods)
        {
            var informationWithMod = new GeneratedInformationWithMod(mod);

            foreach (var generator in desiredGenerators.Where(generator => generator.CanGenerate(mod)))
            {
                informationWithMod.Merge(generator.Generate(mod));
            }

            generatedInformation.Add(informationWithMod);
        }

        var finalizedInformation = new GeneratedInformation();
        foreach (var information in generatedInformation)
        {
            finalizedInformation.Merge(information);
        }

        foreach (var installer in desiredInstallers)
        {
            installer.SetFileModifier(fileModifier);
            installer.Install("", "", finalizedInformation, (_, _) => { });
        }

        return finalizedInformation;
    }

    private List<IGenerator> GetGenerators()
    {
        return new List<IGenerator>() { new FiddleGenerator() };
    }

    private List<IModuleInstaller> GetInstallers()
    {
        return new List<IModuleInstaller>() { new FiddleInstaller() };
    }
}