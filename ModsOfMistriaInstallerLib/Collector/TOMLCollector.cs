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
    
    public GeneratedInformation Collect(IMod mod)
    {
        var generatedInformation = new GeneratedInformation();
        
        var allMeta = mod.GetAllFiles(".meta.toml");
        var allToml = mod.GetAllFiles(".toml")
                        .Where(p => !p.EndsWith(".meta.toml", StringComparison.OrdinalIgnoreCase))
                        .Select(p => GetRelativePath(mod, p))
                        .Where(p => !IsUnderMomiFolder(p) && p != "manifest.toml")
                        .Select(path => new GeneratedTomlItem
                        {
                            FilePath = path,
                            ReadFilePath = path
                        })
                        .ToList();
        
        // Map: baseName (lower) → mutable group builder
        var ungroupedItems = new List<GeneratedTomlItem>();

        foreach (var absolutePath in allMeta)
        {
            var relPath  = GetRelativePath(mod, absolutePath);
            if (IsUnderMomiFolder(relPath)) continue;

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
                ungroupedItems.Add(new GeneratedTomlItem
                {
                    FilePath = relPath,
                    ReadFilePath = relPath
                });
                continue;
            }

            var baseName = spriteName[prefix.Length..];
            
            if (!generatedInformation.AnimationGroups.ContainsKey(baseName))
            {
                generatedInformation.AnimationGroups[baseName] = new AnimationGroup { BaseName = baseName };
            }

            var animationGroup = generatedInformation.AnimationGroups[baseName];

            if (prefix == SprPrefix)
            {
                animationGroup.AnimationMetaRelPath = new GeneratedTomlItem
                {
                    FilePath = relPath,
                    ReadFilePath = relPath
                };

                // Look for a paired PNG next to the .meta.toml
                var pngAbsolute = absolutePath[..^".meta.toml".Length] + ".png";
                if (mod.FileExists(GetRelativePath(mod, pngAbsolute)))
                    animationGroup.PngRelPath = GetRelativePath(mod, pngAbsolute);
            }
            else
            {
                animationGroup.ShapeMetaRelPath = new GeneratedTomlItem
                {
                    FilePath = relPath,
                    ReadFilePath = relPath
                };
            }
        }
        
        // After calling Collect(), .toml / .meta.toml files not part of any group,
        // as paths relative to the mod root.
        generatedInformation.Toml.AddRange([.. allToml, .. ungroupedItems ]);
        
        return generatedInformation;
    }

    // "momi/" files are compact definitions consumed directly by their own
    // generators/installers (outfits, furniture, locations) — they aren't
    // game asset files and shouldn't also be copied verbatim into assets/.
    private static bool IsUnderMomiFolder(string relativePath) =>
        relativePath.Replace('\\', '/').StartsWith("momi/", StringComparison.OrdinalIgnoreCase);

    private static string GetRelativePath(IMod mod, string absolutePath)
    {
        // Normalise both sides to forward slashes so this works for ZipMod
        // (whose "paths" are zip entry names, not file-system paths on disk).
        var normalizedBase = mod.GetBasePath().Replace('\\', '/').TrimEnd('/') + '/';
        var normalizedFull = absolutePath.Replace('\\', '/');

        if (normalizedFull.StartsWith(normalizedBase, StringComparison.OrdinalIgnoreCase))
            return normalizedFull[normalizedBase.Length..];

        return normalizedFull;
    }

    private class GroupBuilder
    {
        public string             BaseName             { get; init; } = "";
        public GeneratedTomlItem? AnimationMetaRelPath { get; set; }
        public string?            PngRelPath           { get; set; }
        public GeneratedTomlItem? ShapeMetaRelPath     { get; set; }
    }
}
