using Newtonsoft.Json.Linq;

namespace Garethp.ModsOfMistriaInstaller;

public class Mod
{
    public string Author;

    public string Name;
    
    public string Version;
    
    public string Location;
    
    public string Id => $"{Author.ToLower()}.{Name.ToLower()}".Replace(" ", "_");

    public static Mod FromManifest(string manifestLocation)
    {
        if (!File.Exists(manifestLocation))
        {
            throw new FileNotFoundException("Could not find the manifest file.");
        }

        if (!manifestLocation.EndsWith("manifest.json"))
        {
            throw new Exception("The manifest file must be named manifest.json.");
        }
        
        var manifest = JObject.Parse(File.ReadAllText(manifestLocation));
        
        if (!manifest.ContainsKey("author"))
        {
            throw new Exception("The manifest must contain an author.");
        }
        
        if (!manifest.ContainsKey("name"))
        {
            throw new Exception("The manifest must contain a name.");
        }
        
        if (!manifest.ContainsKey("version"))
        {
            throw new Exception("The manifest must contain a version.");
        }

        return new Mod
        {
            Name = manifest["name"].ToString(),
            Author = manifest["author"].ToString(),
            Version = manifest["version"].ToString(),
            Location = Path.GetDirectoryName(manifestLocation)
        };
    }
}