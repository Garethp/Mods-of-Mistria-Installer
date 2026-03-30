using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Garethp.ModsOfMistriaInstallerLib.Models.StoreItems;

public class StoreItemConverter : Newtonsoft.Json.Converters.CustomCreationConverter<StoreItem>
{
    public override StoreItem Create(Type objectType)
    {
        throw new NotImplementedException();
    }

    public StoreItem Create(Type objectType, JObject jObject)
    {
        var item = jObject["item"];
        if (item is null) return new StoreItem();
        if (item is JValue) return new SimpleItem();
        if (item is not JObject) return new StoreItem();
        if (item["animal"] is not null) return new AnimalStoreItem();
        if (item["cosmetic"] is not null) return new CosmeticItem();
        if (item["recipe_scroll"] is not null) return new RecipeScrollItem();
        if (item["crafting_scroll"] is not null) return new CraftingScrollStoreItem();

        return new StoreItem();
    }

    public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
    {
        var jObject = JObject.Load(reader);

        // Create target object based on JObject 
        var target = Create(objectType, jObject);

        // Populate the object properties 
        serializer.Populate(jObject.CreateReader(), target);

        return target;
    }
}