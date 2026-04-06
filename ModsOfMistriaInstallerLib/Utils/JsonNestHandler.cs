using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;

namespace Garethp.ModsOfMistriaInstallerLib.Utils;

public static class JsonNestHandler
{
    public static JObject NestTokens(JObject writeObject, JObject referenceObject)
    {
        JToken VisitArray(JArray parent, JArray reference, string path = "")
        {
            for (var key = 0; key < reference.Count; key++)
            {
                var item = parent[key];
                writeObject[$"{path}{key}"] = item;
                
                Visit(item, reference[key],$"{path}{key}/");
            }
            
            return parent;
        }
        
        JToken VisitObject(JObject parent, JObject reference, string path = "")
        {
            var properties = reference.Properties().Select(p => p.Name).ToList();
            foreach (var key in properties)
            {
                var item = parent[key];
                writeObject[$"{path}{key}"] = item;

                if (key.Contains("/")) continue;
                
                Visit(item, reference[key], $"{path}{key}/");
            }
            
            return parent;
        }

        JToken? Visit(JToken parent, JToken reference, string path = "")
        {
            if (parent.Type != reference.Type)
                throw new Exception("Reference object must be the same as the merged object");
            
            return parent switch
            {
                JObject parentObject => VisitObject(parentObject, reference as JObject, path),
                JArray parentArray => VisitArray(parentArray, reference as JArray, path),
                _ => parent
            };
        }

        Visit(writeObject, referenceObject);
        
        var nestedProperties = writeObject
            .Properties()
            .Where(p => p.Name.Contains("/"))
            .Select(p => p.Name)
            .ToList();
        
        foreach (var property in nestedProperties)
        {
            var propertyName = property.Replace("/", ".");
            propertyName = Regex.Replace(propertyName, @"\.([\d]+)\.", "[$1].");
            propertyName = Regex.Replace(propertyName, @"\.([\d]+)$", "[$1]");

            if (property.StartsWith("doors/"))
            {
                var doorKey = property.Substring("doors/".Length);
                propertyName = $"doors['{doorKey.Replace("'", @"\'")}']";
            }
            
            if (writeObject.SelectToken(propertyName) is null)
                writeObject.Remove(property);
        }

        return writeObject;
    }
}