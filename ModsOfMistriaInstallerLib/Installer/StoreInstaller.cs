﻿using Garethp.ModsOfMistriaInstallerLib.Models;
using Newtonsoft.Json.Linq;

namespace Garethp.ModsOfMistriaInstallerLib.Installer;

public class StoreInstaller : ISubModuleInstaller
{
    public JObject Install(
        JObject existingInformation, 
        GeneratedInformation information,
        Action<string, string> reportStatus
    ) {
        if (information.StoreCategories.Count == 0 && information.StoreItems.Count == 0) return existingInformation;

        var stores = existingInformation["stores"];
        if (stores is null) throw new Exception("Could not find stores in __fiddle__.json");

        information.StoreCategories.ForEach(category =>
        {
            var storeName = category.Store;
            var iconName = category.IconName;

            var store = stores[storeName];
            if (store?["categories"] is not JArray categories)
                throw new Exception(
                    $"Could not add category {iconName} to {storeName} because {storeName} does not exist");

            if (categories.Any(existingCategory => existingCategory["icon"]?.ToString() == iconName))
                return;

            var newCategory = new JObject
            {
                { "icon", iconName }
            };
            
            if (category.RandomSelections is not null)
            {
                newCategory["random_selections"] = category.RandomSelections;
                newCategory["random_stock"] = new JArray();
            }
            
            categories.Add(newCategory);
        });
        
        information.StoreItems.ForEach(item =>
        {
            var storeName = item.Store;
            var categoryName = item.Category;
            
            var store = stores[storeName];
            if (store?["categories"] is not JArray categories)
                throw new Exception(
                    $"Failed adding item to the {storeName} {categoryName} category because {storeName} does not exist");
            
            var category = categories.FirstOrDefault(existingCategory => existingCategory["icon"]?.ToString() == categoryName);
            if (category is null) throw new Exception($"Failed adding item to the {storeName} {categoryName} category because {categoryName} does not exist");

            JArray? arrayToAddTo;
            
            if (item.RandomStock)
            {
                if (category["random_stock"] is not JArray)
                    category["random_stock"] = new JArray();
                
                arrayToAddTo = category["random_stock"] as JArray;
            } else if (item.Season is null)
            {
                if (category["constant_stock"] is not JArray)
                    category["constant_stock"] = new JArray();
                
                arrayToAddTo = category["constant_stock"] as JArray;
            } 
            else
            {
                if (category["seasonal"] is not JObject)
                {
                    category["seasonal"] = new JObject
                    {
                        { "winter", new JArray() },
                        { "spring", new JArray() },
                        { "summer", new JArray() },
                        { "fall", new JArray() }
                    };
                }
                
                if (category["seasonal"][item.Season] is not JArray) throw new Exception($"Season {item.Season} does not exist in {storeName} {categoryName}");
                arrayToAddTo = category["seasonal"][item.Season] as JArray;
            }

            if (arrayToAddTo is null) throw new Exception("Could not find array to add item to");
            
            switch (item)
            {
                case SimpleItem simpleItem:
                    arrayToAddTo.Add(new JValue(simpleItem.Item));
                    break;
                case CosmeticItem cosmeticItem:
                    arrayToAddTo.Add(new JObject
                    {
                        { "cosmetic", cosmeticItem.Item.Cosmetic }
                    });
                    break;
                case RecipeScrollItem recipeScrollItem:
                    arrayToAddTo.Add(new JObject
                    {
                        { "recipe_scroll", recipeScrollItem.Item.RecipeScroll }
                    });
                    break;
                default:
                    throw new Exception("Unknown item type");
            }
        });

        return existingInformation;
    }
}