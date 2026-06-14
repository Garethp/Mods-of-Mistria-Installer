using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Tomlyn.Model;

namespace Garethp.ModsOfMistriaInstallerLib.Utils
{
    public class IDManager
    {
        private static HashSet<string> allUsedIds = new();
        public static bool AddUsedId(string id) { return allUsedIds.Add(id); }
        public static HashSet<string> GetAllUsedIds() { return allUsedIds; }
        public static string GenerateUniqueId()
        {
            while (true)
            {
                var id = Convert.ToHexString(
                    SHA256.HashData(Guid.NewGuid().ToByteArray())
                )[..16].ToLowerInvariant();

                if (allUsedIds.Add(id))
                    return id;
            }
        }
        public static HashSet<string> CollectUsedIds(List<Atlas> atlases)
        {

            foreach (var atlas in atlases)
            {
                var data = Toml.LoadToml(atlas.MetaPath);

                if (!data.TryGetValue("asset_properties", out var assetObj))
                    continue;

                var asset = (TomlTable)assetObj;

                if (!asset.TryGetValue("animations", out var animObj))
                    continue;

                foreach (TomlTable anim in (TomlTableArray)animObj)
                {
                    if (!anim.TryGetValue("texture_ids", out var texObj))
                        continue;

                    foreach (var tex in (TomlArray)texObj)
                    {
                        var str = tex.ToString();
                        var id = str.Split("::")[0];
                        allUsedIds.Add(id);
                    }
                }
            }

            return allUsedIds;
        }

    }
}
