using Garethp.ModsOfMistriaInstallerLib.Generator;
using Garethp.ModsOfMistriaInstallerLib.Lang;
using Garethp.ModsOfMistriaInstallerLib.ModTypes;
using Newtonsoft.Json.Linq;

namespace Garethp.ModsOfMistriaInstallerLib.Models.StoreItems;

public class StoreItem
{
    public string Store;
    public string Category;
    public string? Season;
    public bool RandomStock = false;
    public JObject? requirements;

    public Validation Validate(Validation validation, IMod mod, string file)
    {
        if (string.IsNullOrWhiteSpace(Store))
        {
            validation.AddError(mod, file, Resources.CoreErrorStoreItemHasNoStore);
        }

        if (string.IsNullOrWhiteSpace(Category))
        {
            validation.AddError(mod, file, string.Format(Resources.CoreErrorStoreItemHasNoCategory, Store));
        }

        var validSeasons = new List<string> { "spring", "summer", "fall", "winter" };
        if (!string.IsNullOrWhiteSpace(Season) && !validSeasons.Contains(Season))
        {
            validation.AddError(mod, file,
                string.Format(Resources.CoreErrorStoreItemHasInvalidSeason, Store, Category, Season));
        }

        return validation;
    }

    public JObject ToJson()
    {
        var json = new JObject();
        if (requirements is not null) json.Add("requirements", requirements);
        AddJSON(json);

        return json;
    }

    public virtual void AddJSON(JObject json)
    {
        throw new NotImplementedException();
    }
}