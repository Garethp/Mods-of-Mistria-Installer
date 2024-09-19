using Garethp.ModsOfMistriaInstallerLib.Installer.UMT;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Garethp.ModsOfMistriaInstallerLib.Generator;

[InformationGenerator(1)]
public class TilesetGenerator: IGenerator
{
    public GeneratedInformation Generate(Mod mod)
    {
        var modLocation = mod.Location;
        var modId = mod.Id;

        var information = new GeneratedInformation();
        var directory = Path.Combine(mod.Location, "tilesets");

        foreach (var file in Directory.GetFiles(directory).Order())
        {
            var info = JObject.Parse(File.ReadAllText(file));

            foreach (var jsonData in info.Properties())
            {
                if (jsonData.Value is not JValue location)
                {
                    continue;
                }

                if (!File.Exists(Path.Combine(mod.Location, location.ToString()))) continue;
                
                var name = jsonData.Name;

                if (!information.Tilesets.ContainsKey(modId)) information.Tilesets[modId] = [];
                information.Tilesets[modId].Add(new TilesetData
                {
                    Name = name,
                    BaseLocation = modLocation,
                    Location = location.ToString()
                });
            }
        }

        return information;
    }

    public bool CanGenerate(Mod mod)
    {
        return Directory.Exists(Path.Combine(mod.Location, "tilesets"));
    }
    
    public Validation Validate(Mod mod)
    {
        var validation = new Validation();
        if (!CanGenerate(mod)) return validation;

        foreach (var file in Directory.GetFiles(Path.Combine(mod.Location, "tilesets")))
        {
            Dictionary<string, string> tilesets;
            
            try
            {
                tilesets = JsonConvert.DeserializeObject<Dictionary<string, string>>(File.ReadAllText(file));
            } 
            catch (Exception e)
            {
                validation.AddError(mod, file, $"Could not parse file with message: {e.Message}");
                continue;
            }
            
            if (tilesets is null)
            {
                validation.AddError(mod, file, "Could not parse tilesets.");
                continue;
            }
            
            if (tilesets.Count == 0)
            {
                validation.AddWarning(mod, file, "Tileset file has no tilesets.");
            }
            
            foreach (var tilesetName in tilesets.Keys)
            {
                var tilesetLocation = tilesets[tilesetName];
                if (ValidationTools.CheckSpriteFileExists(mod, $"Tileset {tilesetName}", tilesetLocation) is { } error)
                {
                    validation.AddError(mod, file, error);
                }
            }
        }
        
        return validation;
    }
}