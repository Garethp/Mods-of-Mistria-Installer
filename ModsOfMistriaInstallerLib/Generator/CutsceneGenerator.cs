using Garethp.ModsOfMistriaInstallerLib.ModTypes;
using Garethp.ModsOfMistriaInstallerLib.Tools.Compiler;

namespace Garethp.ModsOfMistriaInstallerLib.Generator;

[InformationGenerator(1)]
public class CutsceneGenerator : IGenerator
{
    public GeneratedInformation Generate(IMod mod)
    {
        var cutsceneFiles = mod.GetFilesInFolder("cutscene");

        var generatedInformation = new GeneratedInformation();

        foreach (var cutsceneFile in cutsceneFiles.Order().Where(file => file.EndsWith(".js")))
        {
            var mist = MistCompiler.Compile(mod.ReadFile(cutsceneFile));

            generatedInformation.Cutscenes.Add(mist);
        }

        return generatedInformation;
    }

    public bool CanGenerate(IMod mod) => mod.HasFilesInFolder("cutscene");

    public Validation Validate(IMod mod)
    {
        return new Validation();
    }
}