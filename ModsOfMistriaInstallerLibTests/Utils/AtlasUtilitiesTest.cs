using System.IO.Compression;
using Garethp.ModsOfMistriaInstallerLib.Utils;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using Tomlyn.Model;

namespace ModsOfMistriaInstallerLibTests.Utils;

// Atlas packing against a real zip through ZipFileModifier, the same pair the
// installer drives. Placement entries follow the game's trimmed format:
// [x, y, trimmedW, trimmedH, sourceW, sourceH, offsetX, offsetY].
[TestFixture]
public class AtlasUtilitiesTest
{
    private string _root = "";
    private string _zipPath = "";

    [SetUp]
    public void CreateTempDir()
    {
        _root = Path.Combine(Path.GetTempPath(), "momi_atlas_" + Path.GetRandomFileName());
        Directory.CreateDirectory(_root);
        _zipPath = Path.Combine(_root, "assets.zip");
        IDManager.Reset();
    }

    [TearDown]
    public void RemoveTempDir()
    {
        Directory.Delete(_root, true);
    }

    [Test]
    public void ShouldWriteTrimmedPlacementEntries()
    {
        // Frame 0 is opaque only in a 4×3 box at (2,3); frame 1 is opaque whole
        WithUtils(utils => utils.AddStrip("Default", 8, 8, 2,
            Strip(8, 8, new Rectangle(2, 3, 4, 3), new Rectangle(0, 0, 8, 8)),
            new Dictionary<string, string>(), "test_sprite"));

        var placements = Placements("assets/atlases/DefaultAtlas_0.meta.toml");

        Assert.That(placements, Has.Count.EqualTo(2));
        Assert.That(placements[0], Is.EqualTo(new[] { 1, 1, 4, 3, 8, 8, 2, 3 }));
        Assert.That(placements[1], Is.EqualTo(new[] { 1, 5, 8, 8, 8, 8, 0, 0 }));
    }

    [Test]
    public void ShouldKeepOnePixelForAFullyTransparentFrame()
    {
        WithUtils(utils => utils.AddStrip("Default", 8, 8, 1,
            Strip(8, 8, new Rectangle(0, 0, 0, 0)),
            new Dictionary<string, string>(), "blank_sprite"));

        var placements = Placements("assets/atlases/DefaultAtlas_0.meta.toml");

        Assert.That(placements[0], Is.EqualTo(new[] { 1, 1, 1, 1, 8, 8, 0, 0 }));
    }

    [Test]
    public void ShouldFoldRetiredTypesIntoTheDefaultAtlas()
    {
        foreach (var retired in new[] { "Player", "Shadow", "Animals", "Lut" })
            WithUtils(utils => utils.AddStrip(retired, 4, 4, 1,
                Strip(4, 4, new Rectangle(0, 0, 4, 4)),
                new Dictionary<string, string>(), $"sprite_{retired}"));

        Assert.That(AtlasEntries(), Is.EqualTo(new[]
        {
            "assets/atlases/DefaultAtlas_0.meta.toml",
            "assets/atlases/DefaultAtlas_0.png",
        }));
        Assert.That(Placements("assets/atlases/DefaultAtlas_0.meta.toml"), Has.Count.EqualTo(4));
    }

    [Test]
    public void ShouldKeepACustomAtlasType()
    {
        WithUtils(utils => utils.AddStrip("DeepDungeonWorld", 4, 4, 1,
            Strip(4, 4, new Rectangle(0, 0, 4, 4)),
            new Dictionary<string, string>(), "world_sprite"));

        Assert.That(AtlasEntries(), Does.Contain("assets/atlases/DeepDungeonWorldAtlas_0.meta.toml"));
    }

    [Test]
    public void ShouldPackAroundExistingPlacementEntries()
    {
        Seed("DefaultAtlas_0", """
            [[asset_properties.animations]]
            texture_ids = ["aaaabbbbccccdddd::0"]
            placement = [1, 1, 10, 10, 12, 12, 1, 1]
            """);

        WithUtils(utils => utils.AddStrip("Default", 8, 8, 1,
            Strip(8, 8, new Rectangle(0, 0, 8, 8)),
            new Dictionary<string, string>(), "test_sprite"));

        var placements = Placements("assets/atlases/DefaultAtlas_0.meta.toml");

        Assert.That(placements[0], Is.EqualTo(new[] { 1, 1, 10, 10, 12, 12, 1, 1 }));
        Assert.That(placements[1], Is.EqualTo(new[] { 11, 1, 8, 8, 8, 8, 0, 0 }));
    }

    [Test]
    public void ShouldClearAndRemoveAReplacedPlacementEntry()
    {
        Seed("DefaultAtlas_0", """
            [[asset_properties.animations]]
            texture_ids = ["aaaabbbbccccdddd::0"]
            placement = [1, 1, 4, 4, 8, 8, 2, 2]
            """);

        WithUtils(utils => utils.RemoveById("aaaabbbbccccdddd"));

        Assert.That(Placements("assets/atlases/DefaultAtlas_0.meta.toml"), Is.Empty);

        using var archive = ZipFile.OpenRead(_zipPath);
        using var stream = archive.GetEntry("assets/atlases/DefaultAtlas_0.png")!.Open();
        using var image = Image.Load<Rgba32>(stream);
        Assert.That(image[2, 2].A, Is.EqualTo(0), "the cleared region must be transparent");
    }

    // ── Fixtures and helpers ───────────────────────────────────────────────────

    private void WithUtils(Action<AtlasUtilities> action)
    {
        using var archive = ZipFile.Open(_zipPath, ZipArchiveMode.Update);
        var utils = new AtlasUtilities(Path.Combine("assets", "atlases"), new ZipFileModifier(archive));

        action(utils);

        utils.Flush();
    }

    // A horizontal strip, one opaque box per frame. A zero-size box leaves the
    // frame fully transparent; a full-frame box makes it opaque whole.
    private static MemoryStream Strip(int frameWidth, int frameHeight, params Rectangle[] opaque)
    {
        using var image = new Image<Rgba32>(frameWidth * opaque.Length, frameHeight);
        for (var i = 0; i < opaque.Length; i++)
        {
            var box = opaque[i];
            for (var y = box.Y; y < box.Y + box.Height; y++)
            for (var x = box.X; x < box.X + box.Width; x++)
                image[i * frameWidth + x, y] = new Rgba32(255, 0, 0, 255);
        }

        var stream = new MemoryStream();
        image.SaveAsPng(stream);
        stream.Position = 0;
        return stream;
    }

    // A 32×32 atlas pair: the given animation entries, plus a red block at
    // (1,1)-(4,4) in the png so a cleared region is observable
    private void Seed(string atlasName, string animationsToml)
    {
        using var archive = ZipFile.Open(_zipPath, ZipArchiveMode.Update);

        // The game's archive carries the directory entry; atlas discovery keys on it
        archive.CreateEntry("assets/atlases/");

        using (var writer = new StreamWriter(archive.CreateEntry($"assets/atlases/{atlasName}.meta.toml").Open()))
            writer.Write($"""
                [meta_properties]
                id = "feedfeedfeedfeed"
                asset_kind = "TextureAtlas"

                [asset_properties]
                dimensions = [32, 32]
                filter_kind = "Nearest"
                texture_wrap = "Repeat"
                mipmap_filter_kind = "Nearest"
                srgb = true

                {animationsToml}
                """);

        using var image = new Image<Rgba32>(32, 32);
        for (var y = 1; y <= 4; y++)
        for (var x = 1; x <= 4; x++)
            image[x, y] = new Rgba32(255, 0, 0, 255);

        using var pngStream = archive.CreateEntry($"assets/atlases/{atlasName}.png").Open();
        image.SaveAsPng(pngStream);
    }

    // One list per animation entry: its placement values, or null when the
    // entry carries no placement key (a legacy entry left as read)
    private List<List<int>?> Placements(string metaEntry)
    {
        using var archive = ZipFile.OpenRead(_zipPath);
        using var reader = new StreamReader(archive.GetEntry(metaEntry)!.Open());
        var doc = Toml.ParseToml(reader.ReadToEnd());

        if (doc["asset_properties"] is not TomlTable ap ||
            !ap.TryGetValue("animations", out var animObj) || animObj is not TomlTableArray anims)
            return [];

        return anims
            .Select(a => a.TryGetValue("placement", out var p) && p is TomlArray values
                ? values.Select(Convert.ToInt32).ToList()
                : null)
            .ToList();
    }

    private List<string> AtlasEntries()
    {
        using var archive = ZipFile.OpenRead(_zipPath);
        return archive.Entries
            .Select(e => e.FullName)
            .Where(n => n.StartsWith("assets/atlases/"))
            .Order()
            .ToList();
    }
}
