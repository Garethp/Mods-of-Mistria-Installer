using Newtonsoft.Json;

namespace Garethp.ModsOfMistriaInstallerLib.Models;

class StoreFile
{
    public List<StoreCategory> Categories = [];

    [JsonProperty(ItemConverterType = typeof(StoreItemConverter))]
    public List<StoreItem> Items = [];
}