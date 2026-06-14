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
        private static readonly HashSet<string> allUsedIds = new();

        private static readonly Dictionary<string, HashSet<string>> atlasToIds = new(
            StringComparer.OrdinalIgnoreCase);

        private static readonly Dictionary<string, HashSet<string>> idToAtlases = new(
            StringComparer.OrdinalIgnoreCase);

        public static bool AddUsedId(string id)
        {
            return allUsedIds.Add(id);
        }

        public static HashSet<string> GetAllUsedIds()
        {
            return allUsedIds;
        }

        public static IReadOnlyCollection<string> GetAtlasesContainingId(string id)
        {
            return idToAtlases.TryGetValue(id, out var atlases)
                ? atlases
                : Array.Empty<string>();
        }

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

                if (assetObj is not TomlTable asset)
                    continue;

                if (!asset.TryGetValue("animations", out var animObj))
                    continue;

                if (animObj is not TomlTableArray animations)
                    continue;

                foreach (TomlTable anim in animations)
                {
                    if (!anim.TryGetValue("texture_ids", out var texObj))
                        continue;

                    if (texObj is not TomlArray textureIds)
                        continue;

                    foreach (var tex in textureIds)
                    {
                        var id = tex.ToString()!.Split("::")[0];

                        allUsedIds.Add(id);

                        if (!atlasToIds.TryGetValue(atlas.MetaPath, out var ids))
                        {
                            ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                            atlasToIds[atlas.MetaPath] = ids;
                        }

                        ids.Add(id);

                        if (!idToAtlases.TryGetValue(id, out var atlasPaths))
                        {
                            atlasPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                            idToAtlases[id] = atlasPaths;
                        }

                        atlasPaths.Add(atlas.MetaPath);
                    }
                }
            }

            return allUsedIds;
        }

        
    }
}
