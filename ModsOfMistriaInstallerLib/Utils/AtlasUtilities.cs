using System.Diagnostics;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Advanced;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using Tomlyn;
using Tomlyn.Model;

namespace Garethp.ModsOfMistriaInstallerLib.Utils;

// Manages atlas images and meta.toml files for a given atlas directory.
// Packs animation strips into atlases and registers their frame coordinates.
public class AtlasUtilities
{
    private readonly string _atlasDirectory;
    private readonly List<Atlas> _atlases;

    private readonly IFileModifier _fileModifier;
    private readonly AtlasStateManager _stateManager;

    public AtlasUtilities(string atlasDirectory, IFileModifier fileModifier)
    {
        _atlasDirectory = Path.Combine("assets", "atlases");
        _fileModifier   = fileModifier;
        _atlases        = LoadAtlases();
        _stateManager = new AtlasStateManager(_atlases, _atlasDirectory, _fileModifier);
    }

    public IReadOnlyList<Atlas> GetAtlases() => _atlases;

    // Packs one animation strip (png + its metadata) into the appropriate atlas.
    // Returns the ID assigned to this animation.
    public string AddStrip(
        string atlasType,
        int    frameWidth,
        int    frameHeight,
        int    frameCount,
        Stream pngStream,
        Dictionary<string, string> fileNameUIDMapping,
        string baseName)
    {
        atlasType = Atlas.CanonicalType(atlasType)!;

        var state = _stateManager.OpenState(atlasType);

        // Reuse ID if this animation was already mapped (e.g., replacing a previous mod's version)
        if (!fileNameUIDMapping.TryGetValue(baseName, out var id))
        {
            id = IDManager.GenerateUniqueId();
            fileNameUIDMapping[baseName] = id;
        }

        using var stripImage = Image.Load<Rgba32>(pngStream);
        pngStream.Close();

        for (int i = 0; i < frameCount; i++)
        {
            using var frame = stripImage.Clone(ctx =>
                ctx.Crop(new Rectangle(i * frameWidth, 0, frameWidth, frameHeight)));

            // The game stores frames trimmed: only the opaque bounding box lands
            // in the atlas, and placement carries the authored frame size plus
            // the trim offset so the frame reconstructs at its full size.
            var trim = OpaqueBounds(frame);

            var pos = state.Packer.FindPosition(trim.Width, trim.Height);

            if (pos is null)
            {
                // Current atlas is full — save it (if dirty) and create the next numbered one
                state = _stateManager.GetNextAtlas(atlasType);

                pos = state.Packer.FindPosition(trim.Width, trim.Height);
                if (pos is null)
                    throw new InvalidOperationException(
                        $"Frame ({trim.Width}×{trim.Height}) is too large for a blank atlas.");
            }

            var (x, y) = pos.Value;

            using var trimmed = frame.Clone(ctx => ctx.Crop(trim));
            state.GetImage().Mutate(ctx => ctx.DrawImage(trimmed, new Point(x, y), 1f));
            state.Packer.Add(x, y, trim.Width, trim.Height);

            state.Animations.Add(new TomlTable
            {
                ["texture_ids"] = new TomlArray { $"{id}::{i}" },
                ["placement"]   = new TomlArray { x, y, trim.Width, trim.Height,
                                                  frameWidth, frameHeight, trim.X, trim.Y }
            });

            state.IsDirty = true;
        }

        return id;
    }

    // Removes all atlas entries whose texture_ids contain any frame of baseId
    // (e.g. "abc123" removes "abc123", "abc123::0", "abc123::1" …).
    // Entries that share a pixel region with other IDs are pruned rather than removed.
    // Must be called before AddStrip so open states don't re-introduce the old entries.
    public void RemoveById(string baseId)
    {
        // Strip any ::N suffix the caller may have included
        var prefix = baseId.Contains("::") ? baseId[..baseId.IndexOf("::", StringComparison.Ordinal)] : baseId;

        foreach (var atlas in _stateManager.GetAtlases())
        {
            var state = _stateManager.GetAtlas(atlas.PngPath);
            if (state is null) continue;
            var data = state.Data;

            if (!data.TryGetValue("asset_properties", out var apObj) || apObj is not TomlTable ap) continue;
            if (!ap.TryGetValue("animations", out var animObj) || animObj is not TomlTableArray anims) continue;

            bool modified = false;
            var cleared = new List<Rectangle>();
            PruneAnimations(anims, prefix, ref modified, cleared);

            if (!modified) continue;

            // Sanitise the freed pixel regions in the atlas image so later packs
            // can't reveal the removed art through their transparent areas.
            if (cleared.Count <= 0) continue;
            var image = state.GetImage();

            foreach (var region in cleared)
                ClearRegion(image, region);
        }
    }

    // Removes or prunes animation entries that reference baseId (no ::N suffix).
    // Entries with multiple IDs only have the matching ones removed; if that empties
    // the texture_ids array the whole entry is removed.
    // When an entry is fully removed its pixel region is added to `cleared` so the
    // caller can wipe those pixels from the atlas image — otherwise the slot is freed
    // for the packer while the old art stays behind, and a later sprite packed there
    // shows the stale pixels through its transparent areas.
    private static void PruneAnimations(TomlTableArray anims, string baseId, ref bool modified,
                                        List<Rectangle> cleared)
    {
        for (int i = anims.Count - 1; i >= 0; i--)
        {
            var anim = anims[i];
            if (!anim.TryGetValue("texture_ids", out var idsObj) || idsObj is not TomlArray ids)
                continue;

            var matching = ids
                .Cast<string>()
                .Select((id, idx) => (id, idx))
                .Where(t => string.Equals(t.id, baseId, StringComparison.OrdinalIgnoreCase)
                         || t.id.StartsWith(baseId + "::", StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(t => t.idx)
                .ToList();

            if (matching.Count == 0) continue;

            if (matching.Count == ids.Count)
            {
                // All IDs in this slot belong to the replaced animation → drop the entry
                // and remember its region so the pixels get sanitised.
                if (TryReadRegion(anim, out var region))
                    cleared.Add(region);
                anims.RemoveAt(i);
            }
            else
            {
                // Some other animations share this pixel region → remove only our IDs.
                // The region stays in use, so it must NOT be cleared.
                foreach (var (_, idx) in matching)
                    ids.RemoveAt(idx);
            }

            modified = true;
        }
    }

    // Reads an animation entry's stored atlas rectangle: the first four values
    // of `placement` (the trimmed box).
    private static bool TryReadRegion(TomlTable anim, out Rectangle region)
    {
        region = default;
        if (!anim.TryGetValue("placement", out var dObj) || dObj is not TomlArray d || d.Count < 4)
        {
            return false;
        }

        region = new Rectangle(
            Convert.ToInt32(d[0]), Convert.ToInt32(d[1]),
            Convert.ToInt32(d[2]), Convert.ToInt32(d[3]));
        return true;
    }

    // The smallest rectangle holding every pixel with non-zero alpha.
    // A fully transparent frame keeps a single pixel.
    private static Rectangle OpaqueBounds(Image<Rgba32> frame)
    {
        int minX = frame.Width, minY = frame.Height, maxX = -1, maxY = -1;

        frame.ProcessPixelRows(accessor =>
        {
            for (int y = 0; y < accessor.Height; y++)
            {
                var row = accessor.GetRowSpan(y);
                for (int x = 0; x < row.Length; x++)
                {
                    if (row[x].A == 0) continue;
                    if (x < minX) minX = x;
                    if (x > maxX) maxX = x;
                    if (y < minY) minY = y;
                    maxY = y;
                }
            }
        });

        return maxX < 0
            ? new Rectangle(0, 0, 1, 1)
            : new Rectangle(minX, minY, maxX - minX + 1, maxY - minY + 1);
    }

    // Overwrites a rectangular region of the atlas with fully transparent pixels.
    private static void ClearRegion(Image<Rgba32> image, Rectangle region)
    {
        var r = Rectangle.Intersect(region, new Rectangle(0, 0, image.Width, image.Height));
        if (r.Width <= 0 || r.Height <= 0) return;

        image.ProcessPixelRows(accessor =>
        {
            for (int y = r.Top; y < r.Bottom; y++)
                accessor.GetRowSpan(y).Slice(r.Left, r.Width).Clear();
        });
    }

    // Writes all pending atlas changes to disk and marks them in the manifest.
    public void Flush()
    {
        _stateManager.Flush();
    }

    // This is just to stop memory from running out of control if there's a large number of mods replacing or adding
    // massive numbers of images over a large number of Atlases. We want to keep Atlases open for as long as we can,
    // since we might need to remove items from them at any point, but we don't want to go overboard with how many
    // we have.
    public void SemiFlush()
    {
        _stateManager.SemiFlush();
    }
    
    private List<Atlas> LoadAtlases()
    {
        var atlases = new List<Atlas>();

        if (!_fileModifier.Exists(_atlasDirectory))
            return atlases;

        foreach (var metaPath in _fileModifier.FindFiles(_atlasDirectory, ".meta.toml"))
        {
            // Strip both extensions: file.meta.toml → file
            var stem = Path.GetFileNameWithoutExtension(
                       Path.GetFileNameWithoutExtension(metaPath));

            var parts = stem.Split('_');
            if (parts.Length >= 2 && int.TryParse(parts[^1], out int index))
            {
                var prefix = string.Join('_', parts[..^1]).Replace("Atlas", "");
                atlases.Add(new Atlas(prefix, index, _atlasDirectory, _fileModifier));
                continue;
            }

            if (!stem.EndsWith("Atlas", StringComparison.OrdinalIgnoreCase)) continue;
            var type = stem[..^"Atlas".Length];
            if (type.Length == 0) continue;

            Atlas.RegisterUnnumberedType(type);
            atlases.Add(new Atlas(type, 0, _atlasDirectory, _fileModifier));
        }

        return atlases
            .OrderBy(a => a.Type)
            .ThenBy(a => a.Number)
            .ToList();
    }

    // Reconstructs a ShelfPacker with all existing frame placements from the atlas meta.
    // Reads actual atlas dimensions from asset_properties.dimensions so the packer
    // correctly detects when a small atlas (e.g. 512×512) is full.
    private static ShelfPacker BuildPacker(TomlTable atlasData)
    {
        int width  = Atlas.DefaultSize;
        int height = Atlas.DefaultSize;

        if (!atlasData.TryGetValue("asset_properties", out var apObj) || apObj is not TomlTable ap)
            return new ShelfPacker(width, height);

        if (ap.TryGetValue("dimensions", out var dimsObj) &&
            dimsObj is TomlArray dims && dims.Count >= 2)
        {
            width  = Convert.ToInt32(dims[0]);
            height = Convert.ToInt32(dims[1]);
        }

        var packer = new ShelfPacker(width, height);

        if (ap.TryGetValue("animations", out var animObj) && animObj is TomlTableArray animations)
        {
            foreach (TomlTable anim in animations)
            {
                if (!TryReadRegion(anim, out var region)) continue;
                packer.Add(region.X, region.Y, region.Width, region.Height);
            }
        }

        return packer;
    }

    // Inner types

    private class AtlasStateManager(List<Atlas> atlases, string atlasDirectory, IFileModifier fileModifier)
    {
        // This represents our dictionary of all Atlas' currently open, with the PngPath as the key and the AtlasPackState
        // as the value
        private Dictionary<string, AtlasPackState> _openAtlases = new();
        
        // This is our list of our pointers to the current Atlas per type. The type is the key, the value is the PngPath,
        // which should then be used as the key for `_openAtlases`
        private Dictionary<string, string> _currentAtlasTypes = new();
        private List<Atlas> _atlases = atlases;
        private string _atlasDirectory = atlasDirectory;
        private IFileModifier _fileModifier = fileModifier;
        
        public List<Atlas> GetAtlases() => _atlases;
        
        private Atlas GetLastAtlas(string type)
        {
            var last = _atlases
                .Where(a => string.Equals(a.Type, type, StringComparison.OrdinalIgnoreCase))
                .OrderBy(a => a.Number)
                .LastOrDefault();

            if (last is not null)
                return last;

            return CreateAtlas(type, 0);
        }
        
        private Atlas CreateAtlas(string type, int number)
        {
            var atlas = new Atlas(type, number, _atlasDirectory, _fileModifier);
            atlas.EnsureImageExists();
            atlas.EnsureMetaExists();
            _atlases.Add(atlas);
            return atlas;
        }

        public AtlasPackState OpenState(string atlasType)
        {
            if (_currentAtlasTypes.ContainsKey(atlasType))
            {
                return _openAtlases[_currentAtlasTypes[atlasType]];
            }

            var lastAtlas = GetLastAtlas(atlasType);
            var state = GetAtlas(lastAtlas.PngPath)!;
            
            _currentAtlasTypes[atlasType] = state.Atlas.PngPath;
            return state;
        }

        public AtlasPackState? GetAtlas(string pngPath)
        {
            if (_openAtlases.ContainsKey(pngPath))
            {
                return _openAtlases[pngPath];
            }
            
            var atlas = _atlases.FirstOrDefault(atlas => atlas.PngPath == pngPath);
            if (atlas is null)
            {
                return null;
            }
            
            var data = atlas.LoadData();
            
            // Safely retrieve (or create) the animations array.
            // A newly created atlas serialises an empty TomlTableArray as nothing,
            // so the key may be absent when we read it back.
            if (!data.TryGetValue("asset_properties", out var apObj) || apObj is not TomlTable ap)
            {
                ap = new TomlTable();
                data["asset_properties"] = ap;
            }
            if (!ap.TryGetValue("animations", out var animObj) || animObj is not TomlTableArray anims)
            {
                anims = new TomlTableArray();
                ap["animations"] = anims;
            }

            var packer = BuildPacker(data);

            var state = new AtlasPackState
            {
                Atlas      = atlas,
                Data       = data,
                Packer     = packer,
                Animations = anims,
                IsDirty    = false
            };

            _openAtlases[atlas.PngPath] = state;
            return state;
        }

        public AtlasPackState GetNextAtlas(string atlasType)
        {
            if (!_currentAtlasTypes.ContainsKey(atlasType))
            {
                return OpenState(atlasType);
            }

            var last = _atlases
                .Where(a => string.Equals(a.Type, atlasType, StringComparison.OrdinalIgnoreCase))
                .OrderBy(a => a.Number)
                .LastOrDefault()!;

            CreateAtlas(atlasType, last.Number + 1);
            _currentAtlasTypes.Remove(atlasType);
            return OpenState(atlasType);
        }

        // Writes all pending atlas changes to disk and marks them in the manifest.
        public void Flush()
        {
            foreach (var state in _openAtlases.Values)
            {
                FlushState(state);
            }

            _currentAtlasTypes.Clear();
            _openAtlases.Clear();
        }

        // This is just to stop memory from running out of control if there's a large number of mods replacing or adding
        // massive numbers of images over a large number of Atlases. We want to keep Atlases open for as long as we can,
        // since we might need to remove items from them at any point, but we don't want to go overboard with how many
        // we have.
        public void SemiFlush()
        {
            if (_openAtlases.Count >= 10)
            {
                Flush();
            }
        }
        
        private void FlushState(AtlasPackState state)
        {
            if (state.IsLoaded())
            {
                var writeStream = _fileModifier.GetWriteStream(state.Atlas.PngPath);
                state.GetImage().Save(writeStream, state.GetImage().DetectEncoder(state.Atlas.PngPath));
                writeStream.Close();
                
                state.GetImage().Dispose();
            }
        
            _fileModifier.Write(state.Atlas.MetaPath, TomlSerializer.Serialize(state.Data));
            state.IsDirty = false;
        }
    }
    
    public class AtlasPackState
    {
        public  required Atlas           Atlas;
        public  required TomlTable       Data;
        private          Image<Rgba32>?  Image;
        public  required ShelfPacker     Packer;
        public  required TomlTableArray  Animations;
        public           bool            IsDirty;

        public Image<Rgba32> GetImage()
        {
            if (Image is not null) return Image;
            
            Image = Atlas.LoadImage();
            return Image;
        }

        public bool IsLoaded()
        {
            return Image is not null;
        }
    }

    // Shelf packer: places rectangles into an atlas without overlap.
    // Groups existing items by row (same y) and tries to append to the right.
    // Falls back to a new row below all content.
    public sealed class ShelfPacker
    {
        private readonly int _width;
        private readonly int _height;
        private readonly List<(int x, int y, int w, int h)> _placed = [];

        internal ShelfPacker(int width, int height)
        {
            _width  = width;
            _height = height;
        }

        internal void Add(int x, int y, int w, int h) => _placed.Add((x, y, w, h));

        internal (int x, int y)? FindPosition(int w, int h)
        {
            if (_placed.Count == 0)
                return (1, 1);

            // Try appending to the right of each existing row
            foreach (var row in _placed.GroupBy(p => p.y).OrderBy(g => g.Key))
            {
                int rowHeight = row.Max(p => p.h);
                if (h > rowHeight) continue; // Item is too tall for this shelf

                int nextX = row.Max(p => p.x + p.w);
                if (nextX + w > _width) continue;

                if (!Overlaps(nextX, row.Key, w, h))
                    return (nextX, row.Key);
            }

            // Start a new row below all existing content
            int newY = _placed.Max(p => p.y + p.h) + 1;
            if (newY + h > _height) return null;

            return (1, newY);
        }

        private bool Overlaps(int x, int y, int w, int h) =>
            _placed.Any(p => x < p.x + p.w && x + w > p.x && y < p.y + p.h && y + h > p.y);
    }
}
