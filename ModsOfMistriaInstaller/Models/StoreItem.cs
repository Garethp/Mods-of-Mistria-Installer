using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;

namespace Garethp.ModsOfMistriaInstaller.Models;

class StoreItemConverter : Newtonsoft.Json.Converters.CustomCreationConverter<StoreItem>
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
        if (item["cosmetic"] is not null) return new CosmeticItem();

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

public class CosmeticDefinition
{
    public string Cosmetic;
}

public class StoreItem
{
    public string Store;
    public string Category;
    public string? Season;
}

[JsonObject(NamingStrategyType = typeof(SnakeCaseNamingStrategy))]
public class SimpleItem : StoreItem
{
    public string Item;
}

[JsonObject(NamingStrategyType = typeof(SnakeCaseNamingStrategy))]
public class CosmeticItem : StoreItem
{
    public CosmeticDefinition Item;
}