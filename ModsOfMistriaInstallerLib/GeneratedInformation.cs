
using Garethp.ModsOfMistriaInstallerLib.Installer.UMT;
using Garethp.ModsOfMistriaInstallerLib.Models;
using Newtonsoft.Json.Linq;

namespace Garethp.ModsOfMistriaInstallerLib;


public class GeneratedInformation
{
    public List<JObject> Scripts = [];

    public List<JObject> Localisations = [];
    
    public List<JObject> Fiddles = [];
    
    public List<JObject> Conversations = [];
    
    public List<JObject> Points = [];
    
    public List<JObject> Schedules = [];
    
    public List<JObject> Outlines = [];
    
    public List<JObject> AssetParts = [];

    public Dictionary<string, List<SpriteData>> Sprites = [];

    public Dictionary<string, List<TilesetData>> Tilesets = [];
    
    public List<StoreCategory> StoreCategories = [];
    
    public List<StoreItem> StoreItems = [];

    public List<JObject> Cutscenes = [];

    public List<JObject> ShadowManifests = [];

    public List<AurieMod> AurieMods = [];
    
    public void Merge(GeneratedInformation other)
    {
        Scripts.AddRange(other.Scripts);
        Localisations.AddRange(other.Localisations);
        Fiddles.AddRange(other.Fiddles);
        Conversations.AddRange(other.Conversations);
        Points.AddRange(other.Points);
        Schedules.AddRange(other.Schedules);
        Outlines.AddRange(other.Outlines);
        AssetParts.AddRange(other.AssetParts);
        StoreCategories.AddRange(other.StoreCategories);
        StoreItems.AddRange(other.StoreItems);
        Cutscenes.AddRange(other.Cutscenes);
        ShadowManifests.AddRange(other.ShadowManifests);
        AurieMods.AddRange(other.AurieMods);
        
        foreach (var modName in other.Sprites.Keys)
        {
            if (!Sprites.ContainsKey(modName)) Sprites[modName] = [];
            Sprites[modName].AddRange(other.Sprites[modName]);;
        }
        
        foreach (var modName in other.Tilesets.Keys)
        {
            if (!Tilesets.ContainsKey(modName)) Tilesets[modName] = [];
            Tilesets[modName].AddRange(other.Tilesets[modName]);;
        }
    }
}