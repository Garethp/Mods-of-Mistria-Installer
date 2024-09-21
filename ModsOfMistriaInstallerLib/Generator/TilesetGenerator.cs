using Garethp.ModsOfMistriaInstallerLib.Installer.UMT;
using Garethp.ModsOfMistriaInstallerLib.Lang;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Garethp.ModsOfMistriaInstallerLib.Generator;

[InformationGenerator(1)]
public class TilesetGenerator: IGenerator
{
    public GeneratedInformation Generate(IMod mod)
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

    public bool CanGenerate(IMod mod) => mod.HasFilesInFolder("tilesets");
    
    public Validation Validate(IMod mod)
    {
        var validation = new Validation();
        if (!CanGenerate(mod)) return validation;

        foreach (var file in mod.GetFilesInFolder("tilesets"))
        {
            Dictionary<string, string> tilesets;
            
            try
            {
                tilesets = JsonConvert.DeserializeObject<Dictionary<string, string>>(mod.ReadFile(file));
            } 
            catch (Exception e)
            {
                validation.AddError(mod, file, string.Format(Resources.CouldNotParseJSON, e.Message));
                continue;
            }
            
            if (tilesets is null)
            {
                validation.AddError(mod, file, Resources.NoDataInJSON);
                continue;
            }
            
            if (tilesets.Count == 0)
            {
                validation.AddWarning(mod, file, Resources.TilesetsFileEmpty);
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