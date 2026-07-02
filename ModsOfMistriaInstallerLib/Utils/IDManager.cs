using System.Security.Cryptography;
using Tomlyn;
using Tomlyn.Model;

namespace Garethp.ModsOfMistriaInstallerLib.Utils;

public static class IDManager
{
    private static readonly HashSet<string> AllUsedIds = new();

    // Clears all tracked IDs. Call before each fresh install session.
    public static void Reset() => AllUsedIds.Clear();

    // Marks an ID as in-use without generating it (e.g., pre-existing IDs from mod files).
    public static void RegisterId(string id) => AllUsedIds.Add(id);

    public static string GenerateUniqueId()
    {
        while (true)
        {
            var id = Convert.ToHexString(
                SHA256.HashData(Guid.NewGuid().ToByteArray())
            )[..16].ToLowerInvariant();

            if (AllUsedIds.Add(id))
                return id;
        }
    }

    // Reads every atlas meta.toml and marks all referenced IDs as used
    // so GenerateUniqueId never produces a collision.
    public static void CollectUsedIds(IEnumerable<Atlas> atlases, IFileModifier fileModifier)
    {
        foreach (var atlas in atlases)
        {
            if (!fileModifier.Exists(atlas.MetaPath)) continue;

            var data = TomlSerializer.Deserialize<TomlTable>(fileModifier.Read(atlas.MetaPath));

            if (!data.TryGetValue("asset_properties", out var apObj) ||
                apObj is not TomlTable ap) continue;

            if (!ap.TryGetValue("animations", out var animObj) ||
                animObj is not TomlTableArray animations) continue;

            foreach (TomlTable anim in animations)
            {
                if (!anim.TryGetValue("texture_ids", out var texObj) ||
                    texObj is not TomlArray textureIds) continue;

                foreach (var tex in textureIds)
                    AllUsedIds.Add(tex.ToString()!.Split("::")[0]);
            }
        }
    }
}
