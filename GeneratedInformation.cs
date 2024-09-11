
using Garethp.ModsOfMistriaInstaller.Installer.UMT;
using Newtonsoft.Json.Linq;

namespace Garethp.ModsOfMistriaInstaller;


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

    public List<SpriteData> Sprites = [];
    
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
    }
}