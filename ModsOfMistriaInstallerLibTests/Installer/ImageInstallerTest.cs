using Garethp.ModsOfMistriaInstallerLib;
using Garethp.ModsOfMistriaInstallerLib.Installer;
using Garethp.ModsOfMistriaInstallerLib.Models.SDK;
using Garethp.ModsOfMistriaInstallerLib.Utils;
using ModsOfMistriaInstallerLibTests.Fixtures;
using ModsOfMistriaInstallerLibTests.TestUtils;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using Tomlyn;

namespace ModsOfMistriaInstallerLibTests.Installer;

// Tests the images/replace/ path for atlas-less game sprites. Wrapping textures such as
// the title screen clouds have no `atlas` key and render from their standalone PNG.
[TestFixture]
public class ImageInstallerTest
{
    private const string GameMetaPath = "assets/animations/Title Screen/spr_test_clouds.meta.toml";
    private const string GamePngPath  = "assets/animations/Title Screen/spr_test_clouds.png";

    private const string AtlasLessMeta = """
        [meta_properties]
        id = "19f4c499cafbf498"
        asset_kind = "Animation"

        [asset_properties]
        frame_size = [8, 8]
        """;

    [Test]
    public void ShouldWriteAnAtlasLessReplacementOverTheStandalonePng()
    {
        var pngBytes = MakePng(8, 8);
        var (modifier, statuses) = InstallReplacement(pngBytes, AtlasLessMeta);

        Assert.Multiple(() =>
        {
            Assert.That(modifier.HasBinaryFile(GamePngPath), Is.True,
                "the standalone PNG next to the game meta should be overwritten");
            Assert.That(modifier.GetBinaryFile(GamePngPath), Is.EqualTo(pngBytes),
                "the PNG must land byte-exact");
            Assert.That(statuses, Has.Some.Contains("standalone PNG"),
                "the direct write should be reported, not silent");
        });

        // The rewritten game meta keeps its identity and never gains an atlas.
        var meta = TomlSerializer.Deserialize<SpriteMetaFile>(modifier.GetFile(GameMetaPath));
        Assert.Multiple(() =>
        {
            Assert.That(meta.Meta?.Id, Is.EqualTo("19f4c499cafbf498"));
            Assert.That(meta.Asset?.Atlas, Is.Null);
        });
    }

    [Test]
    public void ShouldResizeTheMetaWhenTheReplacementDiffersAndStillWriteThePng()
    {
        var pngBytes = MakePng(16, 16);
        var (modifier, _) = InstallReplacement(pngBytes, AtlasLessMeta);

        var meta = TomlSerializer.Deserialize<SpriteMetaFile>(modifier.GetFile(GameMetaPath));
        Assert.Multiple(() =>
        {
            Assert.That(modifier.GetBinaryFile(GamePngPath), Is.EqualTo(pngBytes));
            Assert.That(meta.Asset?.FrameWidth, Is.EqualTo(16));
            Assert.That(meta.Asset?.FrameHeight, Is.EqualTo(16));
        });
    }

    [Test]
    public void ShouldStillSkipWhenTheWidthDoesNotDivideByTheFrameCount()
    {
        // A 9px strip with frame_len = 2 still fails the divisibility guard.
        var pngBytes = MakePng(9, 8);
        var (modifier, statuses) = InstallReplacement(pngBytes, """
            [meta_properties]
            id = "19f4c499cafbf498"
            asset_kind = "Animation"

            [asset_properties]
            frame_size = [8, 8]
            frame_len = 2
            """);

        Assert.Multiple(() =>
        {
            Assert.That(modifier.HasBinaryFile(GamePngPath), Is.False);
            Assert.That(statuses, Has.Some.Contains("not divisible"));
        });
    }

    // Installs one images/replace/ mod against a game tree holding a single sprite.
    private static (MockFileModifier, List<string>) InstallReplacement(byte[] pngBytes, string gameMeta)
    {
        var mod = new MockMod(new Dictionary<string, object>
        {
            { "images/replace/spr_test_clouds.png", pngBytes },
        });
        var modifier = new MockFileModifier(new Dictionary<string, string>
        {
            { GameMetaPath, gameMeta },
        });

        var statuses = new List<string>();
        new ImageInstaller(
                new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase),
                new AtlasUtilities("", modifier),
                modifier)
            .Install(mod, new GeneratedInformation(), (status, _) => statuses.Add(status));

        return (modifier, statuses);
    }

    private static byte[] MakePng(int width, int height)
    {
        using var image = new Image<Rgba32>(width, height);
        image[0, 0] = new Rgba32(18, 1, 0, 255);   // A distinctive pixel makes byte equality meaningful.

        using var stream = new MemoryStream();
        image.SaveAsPng(stream);
        return stream.ToArray();
    }
}
