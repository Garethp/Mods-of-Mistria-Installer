using Newtonsoft.Json;

namespace Garethp.ModsOfMistriaInstaller.Models;

class StoreFile
{
    public List<StoreCategory> Categories = [];

    [JsonProperty(ItemConverterType = typeof(StoreItemConverter))]
    public List<StoreItem> Items = [];
}