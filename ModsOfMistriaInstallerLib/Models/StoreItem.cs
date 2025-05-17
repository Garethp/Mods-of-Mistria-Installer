using Garethp.ModsOfMistriaInstallerLib.Generator;
using Garethp.ModsOfMistriaInstallerLib.Lang;
using Garethp.ModsOfMistriaInstallerLib.ModTypes;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;

namespace Garethp.ModsOfMistriaInstallerLib.Models;

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
        if (item["recipe_scroll"] is not null) return new RecipeScrollItem();

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

public class StoreItem
{
    public string Store;
    public string Category;
    public string? Season;
    public bool RandomStock = false;
    
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
            validation.AddError(mod, file, string.Format(Resources.CoreErrorStoreItemHasInvalidSeason, Store, Category, Season));
        }
        
        return validation;
    }
}

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
}

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
}

public class RecipeScrollDefinition
{
    public string RecipeScroll;
}

[JsonObject(NamingStrategyType = typeof(SnakeCaseNamingStrategy))]
public class RecipeScrollItem : StoreItem
{
    public RecipeScrollDefinition Item;
    
    public new Validation Validate(Validation validation, IMod mod, string file)
    {
        validation = base.Validate(validation, mod, file);
        
        if (Item is null || string.IsNullOrWhiteSpace(Item.RecipeScroll))
        {
            validation.AddError(mod, file, string.Format(Resources.CoreErrorStoreItemHasNoItem, Store, Category));
        }
        
        return validation;
    }
}