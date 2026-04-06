using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;

namespace Garethp.ModsOfMistriaInstallerLib.Utils;

public static class JsonNestHandler
{
    public static JToken NestTokens(JObject writeObject, JObject referenceObject)
    {
        var nested = new JObject();
        
        JToken VisitArray(JArray parent, string path = "")
        {
            for (var key = 0; key < parent.Count; key++)
            {
                var item = parent[key];
                nested[$"{path}{key}"] = item;
                
                Visit(item, $"{path}{key}/");
            }
            
            return parent;
        }
        
        JToken VisitObject(JObject parent, string path = "")
        {
            var properties = parent.Properties().Select(p => p.Name).ToList();
            foreach (var key in properties)
            {
                var item = parent[key];
                nested[$"{path}{key}"] = item;

                if (key.Contains("/")) continue;
                
                Visit(item, $"{path}{key}/");
            }
            
            return parent;
        }

        JToken? Visit(JToken? parent, string path = "")
        {
            return parent switch
            {
                JObject parentObject => VisitObject(parentObject, path),
                JArray parentArray => VisitArray(parentArray, path),
                _ => parent
            };
        }

        Visit(writeObject);
        
        var nestedProperties = writeObject
            .Properties()
            .Where(p => p.Name.Contains("/"))
            .Select(p => p.Name)
            .ToList();
        
        foreach (var property in nestedProperties)
        {
            var propertyName = property.Replace("/", ".");
            propertyName = Regex.Replace(propertyName, "\\.([\\d+])", "[$1]");
            if (nested.SelectToken(propertyName) is null)
                nested.Remove(property);
            
            var a = 1 + 1;
        }

        return nested;
    }
}