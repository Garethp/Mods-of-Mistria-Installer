using Garethp.ModsOfMistriaInstaller.Installer.UMT;
using Garethp.ModsOfMistriaInstaller.Models;
using Newtonsoft.Json;

namespace Garethp.ModsOfMistriaInstaller.Installer.Generator;

public class StoreGenerator: IGenerator
{
    public GeneratedInformation Generate(Mod mod)
    {
        var information = new GeneratedInformation();

        foreach (var file in Directory.GetFiles(Path.Combine(mod.Location, "stores")).Order())
        {
            var storeFile = JsonConvert.DeserializeObject<StoreFile>(File.ReadAllText(file));
            if (storeFile is null) throw new Exception($"Attempted to read file {file} but it did not match expected format.");
            
            storeFile.Categories.ForEach(category =>
            {
                if (!information.Sprites.ContainsKey(mod.Id)) information.Sprites[mod.Id] = new();
                information.Sprites[mod.Id].Add(new SpriteData()
                {
                    Name = category.IconName,
                    BaseLocation = mod.Location,
                    Location = category.Sprite,
                    IsUiSprite = true,
                });
            });
            information.StoreCategories.AddRange(storeFile.Categories);
            information.StoreItems.AddRange(storeFile.Items);
        }

        return information;
    }

    public bool CanGenerate(Mod mod) => Directory.Exists(Path.Combine(mod.Location, "stores"));
}