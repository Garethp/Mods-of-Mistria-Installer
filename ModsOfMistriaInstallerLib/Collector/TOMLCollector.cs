using Garethp.ModsOfMistriaInstallerLib.ModTypes;

namespace Garethp.ModsOfMistriaInstallerLib.Collector;

// Scans a mod's files and groups related .meta.toml files by their base name.
// "spr_" prefix → animation asset;  "poly_" prefix → collision shape.
// Files with matching base names (after prefix removal) are returned as an AnimationGroup.
// All other .toml / .meta.toml files are returned via OtherTomlFiles.
public class TOMLCollector
{
    private const string SprPrefix  = "spr_";
    private const string PolyPrefix = "poly_";

    // After calling Collect(), grouped animation+shape pairs.
    public IReadOnlyList<AnimationGroup> Groups { get; private set; } = [];

    // After calling Collect(), .toml / .meta.toml files not part of any group,
    // as paths relative to the mod root.
    public IReadOnlyList<string> OtherTomlFiles { get; private set; } = [];

    public void Collect(IMod mod)
    {
        var allMeta = mod.GetAllFiles(".meta.toml");
        var allToml = mod.GetAllFiles(".toml")
                        .Where(p => !p.EndsWith(".meta.toml", StringComparison.OrdinalIgnoreCase))
                        .ToList();

        // Map: baseName (lower) → mutable group builder
        var builders = new Dictionary<string, GroupBuilder>(StringComparer.OrdinalIgnoreCase);
        var ungroupedMeta = new List<string>();

        foreach (var absolutePath in allMeta)
        {
            var relPath  = GetRelativePath(mod, absolutePath);
            var fileName = Path.GetFileName(absolutePath); // e.g. "spr_foo_idle_east.meta.toml"

            // Strip ".meta.toml" to get just the sprite name
            var spriteName = fileName[..^".meta.toml".Length];

            string? prefix = null;
            if (spriteName.StartsWith(SprPrefix, StringComparison.OrdinalIgnoreCase))
                prefix = SprPrefix;
            else if (spriteName.StartsWith(PolyPrefix, StringComparison.OrdinalIgnoreCase))
                prefix = PolyPrefix;

            if (prefix is null)
            {
                ungroupedMeta.Add(relPath);
                continue;
            }

            var baseName = spriteName[prefix.Length..];

            if (!builders.TryGetValue(baseName, out var builder))
            {
                builder = new GroupBuilder { BaseName = baseName };
                builders[baseName] = builder;
            }

            if (prefix == SprPrefix)
            {
                builder.AnimationMetaRelPath = relPath;

                // Look for a paired PNG next to the .meta.toml
                var pngAbsolute = absolutePath[..^".meta.toml".Length] + ".png";
                if (mod.FileExists(GetRelativePath(mod, pngAbsolute)))
                    builder.PngRelPath = GetRelativePath(mod, pngAbsolute);
            }
            else
            {
                builder.ShapeMetaRelPath = relPath;
            }
        }

        Groups = builders.Values
            .Select(b => new AnimationGroup
            {
                BaseName             = b.BaseName,
                AnimationMetaRelPath = b.AnimationMetaRelPath,
                PngRelPath           = b.PngRelPath,
                ShapeMetaRelPath     = b.ShapeMetaRelPath
            })
            .ToList();

        OtherTomlFiles = [.. allToml, .. ungroupedMeta];
    }

    private static string GetRelativePath(IMod mod, string absolutePath)
    {
        var basePath = mod.GetBasePath();
        return Path.GetRelativePath(basePath, absolutePath);
    }

    private class GroupBuilder
    {
        public string  BaseName             { get; init; } = "";
        public string? AnimationMetaRelPath { get; set; }
        public string? PngRelPath           { get; set; }
        public string? ShapeMetaRelPath     { get; set; }
    }
}
