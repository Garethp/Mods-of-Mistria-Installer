using Garethp.ModsOfMistriaInstallerLib.Installer.UMT;
using Garethp.ModsOfMistriaInstallerLib.Lang;
using Garethp.ModsOfMistriaInstallerLib.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Garethp.ModsOfMistriaInstallerLib.Generator;

[InformationGenerator(1)]
public class ShadowGenerator : IGenerator
{
    public GeneratedInformation Generate(IMod mod)
    {
        var information = new GeneratedInformation();
        
        foreach (var file in Directory.GetFiles(Path.Combine(mod.Location, "shadows")).Order())
        {
            var shadowFile = JsonConvert.DeserializeObject<Dictionary<string, ShadowSprite>>(File.ReadAllText(file));
            if (shadowFile is null) throw new Exception($"Attempted to read file {file} but it did not match expected format.");

            foreach (var shadowName in shadowFile.Keys)
            {
                var shadowSprite = shadowFile[shadowName];
                shadowSprite.Name = shadowName;
                if (!information.Sprites.ContainsKey(mod.Id)) information.Sprites[mod.Id] = new();

                information.Sprites[mod.Id].Add(new SpriteData()
                {
                    Name = shadowSprite.Name,
                    BaseLocation = mod.Location,
                    Location = shadowSprite.Sprite,
                    IsAnimated = shadowSprite.IsAnimated,
                });

                information.ShadowManifests.Add(new JObject
                {
                    { shadowSprite.RegularSpriteName, shadowSprite.Name },
                });
            }
        }

        return information;

    }

    public bool CanGenerate(IMod mod) => mod.HasFilesInFolder("shadows");

    public Validation Validate(IMod mod)
    {
        var validation = new Validation();

        if (!CanGenerate(mod)) return validation;
        
        foreach (var file in mod.GetFilesInFolder("shadows"))
        {
            Dictionary<string, ShadowSprite>? shadowSprites;
            try
            {
                shadowSprites = JsonConvert.DeserializeObject<Dictionary<string, ShadowSprite>>(mod.ReadFile(file));
            }
            catch (Exception e)
            {
                validation.AddError(mod, file, string.Format(Resources.CouldNotParseJSON, e.Message));
                continue;
            }
            
            if (shadowSprites is null)
            {
                validation.AddError(mod, file, Resources.NoDataInJSON);
                continue;
            }

            if (shadowSprites.Count == 0)
            {
                validation.AddWarning(mod, file, Resources.WarningShadowFileNoShadows);
            }

            foreach (var shadowName in shadowSprites.Keys)
            {
                var shadow = shadowSprites[shadowName];
                shadow.Name = shadowName;
                validation = shadow.Validate(validation, mod, file, shadowName);
            }
        }

        return validation;
    }
}