using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Garethp.ModsOfMistriaInstaller.Models;

[JsonObject(NamingStrategyType = typeof(SnakeCaseNamingStrategy))]
public class StoreCategory
{
    public string IconName;

    public string Store;

    public string Sprite;
}