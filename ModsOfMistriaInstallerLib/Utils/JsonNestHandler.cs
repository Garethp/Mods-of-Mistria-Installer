using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;

namespace Garethp.ModsOfMistriaInstallerLib.Utils;

public static class JsonNestHandler
{
    public static JObject NestTokens(JObject writeObject, JObject referenceObject)
    {
        List<string> topLevelKeysToSkip = ["items", "object_prototypes", "fonts"];
        
        JToken VisitArray(JArray parent, JArray reference, string path = "")
        {
            // For objects we only want to nest keys from the refence object, but for arrays we have the ability to append
            // new keys on to the array. So key 0 in the reference might be key 5 in the real object. For this reason we're
            // just going to ditch using the reference once we hit arrays. It's just there for performance, and when tested
            // specifically with arrays we're only looking at ~90ms difference. There would be about 900ms difference if
            // we didn't use reference keys at all anywhere though.
            for (var key = 0; key < parent.Count; key++)
            {
                var item = parent[key];
                writeObject[$"{path}{key}"] = item;
                
                Visit(item, parent[key],$"{path}{key}/");
            }
            
            return parent;
        }
        
        JToken VisitObject(JObject parent, JObject reference, string path = "")
        {
            var properties = reference.Properties().Select(p => p.Name).ToList();
            foreach (var key in properties)
            {
                if (path == "" && topLevelKeysToSkip.Contains(key)) continue;
                
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