using Garethp.ModsOfMistriaInstallerLib.Collector;
using Garethp.ModsOfMistriaInstallerLib.Lang;
using Garethp.ModsOfMistriaInstallerLib.Models.MOMI;
using Garethp.ModsOfMistriaInstallerLib.ModTypes;
using Tomlyn;

namespace Garethp.ModsOfMistriaInstallerLib.Generator;

[InformationGenerator(2)]
public class SpriteGenerator: IGenerator
{
    public GeneratedInformation Generate(IMod mod)
    {
        var information = new GeneratedInformation();

        foreach (var file in mod.GetFilesInFolder("momi/sprites"))
        {
            var sprites = TomlSerializer.Deserialize<Dictionary<string, SpriteToml>>(mod.ReadFile(file));
            
            foreach (var spriteId in sprites.Keys)
            {
                var sprite = sprites[spriteId];
                sprite.Id = spriteId;
                
                // @TODO: Add the meta and poly files
                information.AnimationGroups[spriteId] = new AnimationGroup
                {
                    BaseName = sprite.Id,
                    PngRelPath = sprite.Location,
                };
            }
        }

        return information;
    }

    public bool CanGenerate(IMod mod) => mod.HasFilesInFolder("momi/sprites");

    public Validation Validate(IMod mod)
    {
        var validation = new Validation();
        if (!CanGenerate(mod)) return validation;

        foreach (var file in mod.GetFilesInFolder("momi/sprites"))
        {
            Dictionary<string, SpriteToml>? sprites;
            try
            {
                sprites = TomlSerializer.Deserialize<Dictionary<string, SpriteToml>>(mod.ReadFile(file));
            }
            catch (Exception e)
            {
                validation.AddError(mod, file, string.Format(Resources.CoreCouldNotParseFile, e.Message));
                continue;
            }
            
            if (sprites is null)
            {
                validation.AddError(mod, file, Resources.CoreNoDataInFile);
                continue;
            }
            
            if (sprites.Count == 0)
            {
                validation.AddWarning(mod, file, Resources.CoreSpriteFileHasNoSprites);
            }

            foreach (var spriteId in sprites.Keys)
            {
                var sprite = sprites[spriteId];
                sprite.Id = spriteId;

                validation = sprite.Validate(validation, mod, file);
            }
        }
        
        return validation;
    }
}