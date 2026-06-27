using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using Tomlyn.Model;

namespace Garethp.ModsOfMistriaInstallerLib.Utils;

// Manages atlas images and meta.toml files for a given atlas directory.
// Packs animation strips into atlases and registers their frame coordinates.
public class AtlasUtilities
{
    private readonly string _atlasDirectory;
    private readonly InstallManifest _manifest;
    private readonly List<Atlas> _atlases;
    private readonly Dictionary<string, AtlasPackState> _states = new(StringComparer.OrdinalIgnoreCase);
    // Atlas meta paths created during this session (did not exist before install).
    private readonly HashSet<string> _newAtlasPaths = new(StringComparer.OrdinalIgnoreCase);

    public AtlasUtilities(string atlasDirectory, InstallManifest manifest)
    {
        _atlasDirectory = atlasDirectory;
        _manifest       = manifest;
        _atlases        = LoadAtlases();
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
        if (!_states.TryGetValue(atlasType, out var state))
            state = OpenState(atlasType);

        // Reuse ID if this animation was already mapped (e.g., replacing a previous mod's version)
        if (!fileNameUIDMapping.TryGetValue(baseName, out var id))
        {
            id = IDManager.GenerateUniqueId();
            fileNameUIDMapping[baseName] = id;
        }

        using var stripImage = Image.Load<Rgba32>(pngStream);

        for (int i = 0; i < frameCount; i++)
        {
            var pos = state.Packer.FindPosition(frameWidth, frameHeight);

            if (pos is null)
            {
                // Current atlas is full — save it (if dirty) and create the next numbered one
                int nextNumber = state.Atlas.Number + 1;
                if (state.IsDirty) FlushState(state);
                else state.Image.Dispose();
                _states.Remove(atlasType);
                CreateAtlas(atlasType, nextNumber);
                state = OpenState(atlasType);
                _states[atlasType] = state;

                pos = state.Packer.FindPosition(frameWidth, frameHeight);
                if (pos is null)
                    throw new InvalidOperationException(
                        $"Frame ({frameWidth}×{frameHeight}) is too large for a blank atlas.");
            }

            var (x, y) = pos.Value;

            using var frame = stripImage.Clone(ctx =>
                ctx.Crop(new Rectangle(i * frameWidth, 0, frameWidth, frameHeight)));

            state.Image.Mutate(ctx => ctx.DrawImage(frame, new Point(x, y), 1f));
            state.Packer.Add(x, y, frameWidth, frameHeight);

            state.Animations.Add(new TomlTable
            {
                ["texture_ids"]        = new TomlArray { $"{id}::{i}" },
                ["top_left_dimensions"] = new TomlArray { x, y, frameWidth, frameHeight }
            });

            state.IsDirty = true;
        }

        return id;
    }

    // Writes all pending atlas changes to disk and marks them in the manifest.
    public void Flush()
    {
        foreach (var state in _states.Values)
        {
            if (!state.IsDirty) continue;
            FlushState(state);
        }
        _states.Clear();
    }

    // Private helpers

    private AtlasPackState OpenState(string atlasType)
    {
        var atlas = GetLastAtlas(atlasType);

        var data  = Toml.LoadToml(atlas.MetaPath);
        var image = Image.Load<Rgba32>(atlas.PngPath);

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
            Image      = image,
            Packer     = packer,
            Animations = anims,
            IsDirty    = false
        };

        _states[atlasType] = state;
        return state;
    }

    private void FlushState(AtlasPackState state)
    {
        // Atlases created during this session are "added" (no original to restore);
        // pre-existing atlases are "modified" and need a backup for uninstall.
        if (_newAtlasPaths.Contains(state.Atlas.MetaPath))
        {
            _manifest.TrackAdded(state.Atlas.PngPath);
            _manifest.TrackAdded(state.Atlas.MetaPath);
        }
        else
        {
            _manifest.TrackModified(state.Atlas.PngPath);
            _manifest.TrackModified(state.Atlas.MetaPath);
        }

        state.Image.Save(state.Atlas.PngPath);
        Toml.SaveToml(state.Data, state.Atlas.MetaPath);
        state.Image.Dispose();
        state.IsDirty = false;
    }

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
        var atlas = new Atlas(type, number, _atlasDirectory);
        atlas.EnsureImageExists();
        atlas.EnsureMetaExists();
        _atlases.Add(atlas);
        _newAtlasPaths.Add(atlas.MetaPath);
        return atlas;
    }

    private List<Atlas> LoadAtlases()
    {
        var atlases = new List<Atlas>();

        if (!Directory.Exists(_atlasDirectory))
            return atlases;

        foreach (var metaPath in Directory.GetFiles(_atlasDirectory, "*.meta.toml"))
        {
            // Strip both extensions: file.meta.toml → file
            var stem = Path.GetFileNameWithoutExtension(
                       Path.GetFileNameWithoutExtension(metaPath));

            // ShadowAtlas (no number suffix) is a special case
            if (stem.Equals("ShadowAtlas", StringComparison.OrdinalIgnoreCase))
            {
                atlases.Add(new Atlas("Shadow", 0, _atlasDirectory));
                continue;
            }

            // Expected pattern: TypeAtlas_N
            var parts = stem.Split('_');
            if (parts.Length < 2) continue;
            if (!int.TryParse(parts[^1], out int index)) continue;

            var prefix = string.Join('_', parts[..^1]).Replace("Atlas", "");
            atlases.Add(new Atlas(prefix, index, _atlasDirectory));
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
                if (!anim.TryGetValue("top_left_dimensions", out var dimObj) ||
                    dimObj is not TomlArray d || d.Count < 4) continue;

                packer.Add(
                    Convert.ToInt32(d[0]),
                    Convert.ToInt32(d[1]),
                    Convert.ToInt32(d[2]),
                    Convert.ToInt32(d[3]));
            }
        }

        return packer;
    }

    // Inner types

    private class AtlasPackState
    {
        public required Atlas           Atlas;
        public required TomlTable       Data;
        public required Image<Rgba32>   Image;
        public required ShelfPacker     Packer;
        public required TomlTableArray  Animations;
        public          bool            IsDirty;
    }

    // Shelf packer: places rectangles into an atlas without overlap.
    // Groups existing items by row (same y) and tries to append to the right.
    // Falls back to a new row below all content.
    internal sealed class ShelfPacker
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
