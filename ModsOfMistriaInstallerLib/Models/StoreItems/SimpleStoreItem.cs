using Garethp.ModsOfMistriaInstallerLib.Generator;
using Garethp.ModsOfMistriaInstallerLib.Lang;
using Garethp.ModsOfMistriaInstallerLib.ModTypes;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;

namespace Garethp.ModsOfMistriaInstallerLib.Models.StoreItems;

[JsonObject(NamingStrategyType = typeof(SnakeCaseNamingStrategy))]
public class SimpleItem : StoreItem
{
    public string Item;

    public new Validation Validate(Validation validation, IMod mod, string file)
    {
        validation = base.Validate(validation, mod, file);

        if (string.IsNullOrWhiteSpace(Item))
        {
            validation.AddError(mod, file, string.Format(Resources.CoreErrorStoreItemHasNoItem, Store, Category));
        }

        return validation;
    }

    public override void AddJson(JObject json) => json.Add("item", Item);
}