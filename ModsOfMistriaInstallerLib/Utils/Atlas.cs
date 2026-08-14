using Garethp.ModsOfMistriaInstallerLib.Models;
using Garethp.ModsOfMistriaInstallerLib.Models.SDK;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Advanced;
using SixLabors.ImageSharp.PixelFormats;
using Tomlyn;

namespace Garethp.ModsOfMistriaInstallerLib.Utils;

// Represents a single atlas image + meta.toml pair on disk.
// Does not manage lists — that belongs to AtlasUtilities.
public class Atlas
{
    public const int DefaultSize = 4096;

    // The atlas category, e.g. "Animals", "Npc", "Shadow"
    public string Type { get; }

    // 0-based index. For ShadowAtlas the original file has no number suffix (index 0).
    public int Number { get; }

    public string MetaPath { get; }
    public string PngPath  { get; }
    public int Width       { get; }
    public int Height      { get; }
    
    private IFileModifier _fileModifier;

    public Atlas(string type, int number, string atlasDirectory, IFileModifier fileModifier, int width = DefaultSize, int height = DefaultSize)
    {
        Type      = type;
        Number    = number;
        MetaPath  = BuildMetaName(type, number, atlasDirectory);
        PngPath   = BuildPngName(type, number, atlasDirectory);
        Width     = width;
        Height    = height;
        _fileModifier = fileModifier;
    }

    private static readonly HashSet<string> UnnumberedTypes =
        new(StringComparer.OrdinalIgnoreCase) { "Shadow", "Lut", "UI" };

    public static void RegisterUnnumberedType(string type) => UnnumberedTypes.Add(type);

    // The game's atlas consolidation retired these categories: their sprites
    // and atlas files live in Default now. Mod metas naming them keep working.
    private static readonly HashSet<string> RetiredTypes =
        new(StringComparer.OrdinalIgnoreCase) { "Shadow", "Animals", "Player", "Lut" };

    public static string? CanonicalType(string? type)
    {
        if (type is null) return null;
        
        return RetiredTypes.Contains(type) ? "Default" : type;
    }

    private static bool IsUnnumbered(string type, int number) =>
        number == 0 && UnnumberedTypes.Contains(type);

    public static string BuildMetaName(string type, int number, string atlasDirectory)
    {
        var name = IsUnnumbered(type, number)
            ? $"{type}Atlas.meta.toml"
            : $"{type}Atlas_{number}.meta.toml";
        return Path.Combine(atlasDirectory, name);
    }

    public static string BuildPngName(string type, int number, string atlasDirectory)
    {
        var name = IsUnnumbered(type, number)
            ? $"{type}Atlas.png"
            : $"{type}Atlas_{number}.png";
        return Path.Combine(atlasDirectory, name);
    }

    public override bool Equals(object? obj) =>
        obj is Atlas other && Type == other.Type && Number == other.Number;

    public override int GetHashCode() => HashCode.Combine(Type, Number);

    // Creates the atlas image file if it doesn't exist.
    public bool EnsureImageExists()
    {
        if (_fileModifier.Exists(PngPath)) return true;
        using var img = new Image<Rgba32>(Width, Height);
        var imageStream = _fileModifier.GetWriteStream(PngPath);
        img.Save(imageStream, img.DetectEncoder(PngPath));
        imageStream.Close();
        
        return true;
    }

    // Creates the atlas meta.toml file if it doesn't exist. Mods only name
    // the atlas a sprite goes to. They never choose display settings. A new
    // page copies its settings from the page it extends, and that chain
    // always starts at one of the game's own atlases. This means new pages
    // match whatever game version is installed, even when an update changes
    // the settings. Game 1.0.3 flipped srgb on every atlas. The values below
    // apply only when a mod names an atlas the game has no file for, and
    // they match the current game.
    public bool EnsureMetaExists(AtlasAssetProperties? template = null)
    {
        if (_fileModifier.Exists(MetaPath)) return true;
        var data = new AtlasMetaFile
        {
            Meta = new MetaProperties
            {
                Id = IDManager.GenerateUniqueId(),
                AssetKind = "TextureAtlas"
            },
            Asset = new AtlasAssetProperties
            {
                Dimensions = [ Width, Height ],
                Filter = template is null ? "Nearest" : template.Filter,
                MipmapFilter = template is null ? "Nearest" : template.MipmapFilter,
                TextureWrap = template is null ? "Repeat" : template.TextureWrap,
                Srgb = template is null ? false : template.Srgb,
                Animations = []
            }
        };

        _fileModifier.Write(MetaPath, TomlSerializer.Serialize(data));
        return true;
    }

    public AtlasMetaFile LoadData()
    {
        EnsureMetaExists();
        return TomlSerializer.Deserialize<AtlasMetaFile>(_fileModifier.Read(MetaPath))!;
    }

    public Image<Rgba32> LoadImage()
    {
        EnsureImageExists();
        var imageStream = _fileModifier.GetReadStream(PngPath);
        var image = Image.Load<Rgba32>(imageStream);
        imageStream.Close();

        return image;
    }
}
