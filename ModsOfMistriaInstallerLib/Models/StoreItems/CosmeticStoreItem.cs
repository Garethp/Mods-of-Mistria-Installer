using Garethp.ModsOfMistriaInstallerLib.Generator;
using Garethp.ModsOfMistriaInstallerLib.Lang;
using Garethp.ModsOfMistriaInstallerLib.ModTypes;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;

namespace Garethp.ModsOfMistriaInstallerLib.Models.StoreItems;

public class CosmeticDefinition
{
    public string Cosmetic;
}

[JsonObject(NamingStrategyType = typeof(SnakeCaseNamingStrategy))]
public class CosmeticItem : StoreItem
{
    public CosmeticDefinition Item;

    public new Validation Validate(Validation validation, IMod mod, string file)
    {
        validation = base.Validate(validation, mod, file);

        if (Item is null || string.IsNullOrWhiteSpace(Item.Cosmetic))
        {
            validation.AddError(mod, file, string.Format(Resources.CoreErrorStoreItemHasNoItem, Store, Category));
        }

        return validation;
    }

    public override void AddJSON(JObject json) => json.Add("cosmetic", Item.Cosmetic);
}
