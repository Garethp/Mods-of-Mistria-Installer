using Garethp.ModsOfMistriaInstallerLib.Generator;
using Garethp.ModsOfMistriaInstallerLib.Lang;
using Garethp.ModsOfMistriaInstallerLib.ModTypes;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;

namespace Garethp.ModsOfMistriaInstallerLib.Models.StoreItems;

[JsonObject(NamingStrategyType = typeof(SnakeCaseNamingStrategy))]
public class CraftingScrollItemDefinition
{
    public string CraftingScroll;
}

[JsonObject(NamingStrategyType = typeof(SnakeCaseNamingStrategy))]
public class CraftingScrollStoreItem: StoreItem
{
    public CraftingScrollItemDefinition Item;

    public new Validation Validate(Validation validation, IMod mod, string file)
    {
        validation = base.Validate(validation, mod, file);

        if (Item is null || string.IsNullOrWhiteSpace(Item.CraftingScroll))
        {
            validation.AddError(mod, file, string.Format(Resources.CoreErrorStoreItemHasNoItem, Store, Category));
        }

        return validation;
    }
    
    public override void AddJSON(JObject json) => json.Add("crafting_scroll", Item.CraftingScroll);
}