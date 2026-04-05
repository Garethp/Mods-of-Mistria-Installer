using Newtonsoft.Json.Linq;

namespace Garethp.ModsOfMistriaInstallerLib.Utils;

public static class JsonNestHandler
{
    public static JToken NestTokens(JObject jObject)
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

        Visit(jObject);

        return nested;
    }
}