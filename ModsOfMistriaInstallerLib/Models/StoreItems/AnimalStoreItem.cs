using Garethp.ModsOfMistriaInstallerLib.Generator;
using Garethp.ModsOfMistriaInstallerLib.Lang;
using Garethp.ModsOfMistriaInstallerLib.ModTypes;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;

namespace Garethp.ModsOfMistriaInstallerLib.Models.StoreItems;

[JsonObject(NamingStrategyType = typeof(SnakeCaseNamingStrategy))]
public class AnimalItemDefinition
{
    public string Animal;
    public string Cosmetic;
}

[JsonObject(NamingStrategyType = typeof(SnakeCaseNamingStrategy))]
public class AnimalStoreItem : StoreItem
{
    public AnimalItemDefinition Item;

    public new Validation Validate(Validation validation, IMod mod, string file)
    {
        validation = base.Validate(validation, mod, file);

        if (Item is null || string.IsNullOrWhiteSpace(Item.Animal))
        {
            validation.AddError(mod, file, string.Format(Resources.CoreErrorStoreItemHasNoItem, Store, Category));
        }

        if (Item is not null && !string.IsNullOrWhiteSpace(Item.Animal) && string.IsNullOrWhiteSpace(Item.Cosmetic))
        {
            validation.AddError(mod, file, string.Format(Resources.CoreErrorStoreItemAnimalHasNoCosmetic, Item.Animal, Store));
        }

        return validation;
    }
    
    public override void AddJson(JObject json)
    {
        json.Add("animal", Item.Animal);
        json.Add("cosmetic", Item.Cosmetic);
    }
}