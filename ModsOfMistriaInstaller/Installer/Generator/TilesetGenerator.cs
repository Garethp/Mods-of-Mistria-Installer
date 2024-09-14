using Garethp.ModsOfMistriaInstaller.Installer.UMT;
using Newtonsoft.Json.Linq;

namespace Garethp.ModsOfMistriaInstaller.Installer.Generator;

public class TilesetGenerator: IGenerator
{
    public GeneratedInformation Generate(Mod mod)
    {
        var modLocation = mod.Location;
        var modId = mod.Id;

        var information = new GeneratedInformation();
        var directory = Path.Combine(mod.Location, "tilesets");

        foreach (var file in Directory.GetFiles(directory))
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
}