using System.Runtime.InteropServices;
using Garethp.ModsOfMistriaInstallerLib.Lang;
using Garethp.ModsOfMistriaInstallerLib.Models;

namespace Garethp.ModsOfMistriaInstallerLib.Generator;

[InformationGenerator(1)]
public class AurieGenerator: IGenerator
{
    public GeneratedInformation Generate(Mod mod)
    {
        var information = new GeneratedInformation();
        
        foreach (var file in Directory.GetFiles(Path.Combine(mod.Location, "Aurie"), "*.dll"))
        {
            var fileName = Path.GetFileName(file);
            
            information.AurieMods.Add(new AurieMod
            {
                Mod = mod,
                FileName = fileName,
                Location = file
            });
        }

        return information;
    }

    public bool CanGenerate(Mod mod) => Directory.Exists(Path.Combine(mod.Location, "Aurie"));

    public Validation Validate(Mod mod)
    {
        var validation = new Validation();
        if (!CanGenerate(mod)) return validation;

        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            validation.AddError(mod, "", Resources.ErrorModRequiresWindows);
        }
        
        return validation;
    }
}