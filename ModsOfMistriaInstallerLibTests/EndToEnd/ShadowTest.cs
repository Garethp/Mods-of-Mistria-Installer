using Garethp.ModsOfMistriaInstallerLib.Generator;
using Garethp.ModsOfMistriaInstallerLib.Installer;
using Garethp.ModsOfMistriaInstallerLib.Installer.UMT;
using Garethp.ModsOfMistriaInstallerLib.ModTypes;
using ModsOfMistriaInstallerLibTests.Fixtures;
using ModsOfMistriaInstallerLibTests.TestUtils;
using Newtonsoft.Json.Linq;

namespace ModsOfMistriaInstallerLibTests.EndToEnd;

[TestFixture]
public class ShadowTest
{
    private IMod _mockMod;
    private MockFileModifier _fileModifier;
    
    private readonly MockInstaller _installer = new([
        new ShadowGenerator()
    ], [
        new ShadowManifestInstaller(),
    ]);

    [SetUp]
    public void SetUp()
    {
        _fileModifier = new(new Dictionary<string, string>
        {
            { "shadow_manifest.json", "{}" }
        });
        
        _mockMod = new MockMod(new Dictionary<string, string> {
            { "shadows/shadow.json", new JObject {
                { "test_shadow", new JObject {
                    { "regular_sprite_name", "test_sprite" },
                    { "location", "images/sprite.png" },
                    { "is_animated", false }
                }}
            }.ToString() },
            { "images/sprite.png", "" },
        });
    }

    [Test]
    public void ShouldAddSpriteImage()
    {
        var mod = _mockMod;
        var generatedInformation = _installer.InstallMods([mod], _fileModifier);
        
        Assert.That(generatedInformation.Sprites, Contains.Key(mod.GetId()));
        Assert.That(
            generatedInformation.Sprites[mod.GetId()], 
            Has.Some.Matches((SpriteData sprite) => 
                sprite.Name == "test_shadow" &&
                sprite.Location == "images/sprite.png" &&
                !sprite.IsAnimated
            )
        );
    }

    [Test]
    public void ShouldAddShadowManifest()
    {
        _installer.InstallMods([_mockMod], _fileModifier);
        
        Assert.That(_fileModifier.GetFile("shadow_manifest.json"), new MatchesJsonConstraint(new JObject
        {
            { "test_sprite", "test_shadow" }
        }));
    }

    [Test]
    public void ShouldAllowSettingOriginAndMargin()
    {
        var mod = new MockMod(new Dictionary<string, string> {
            { "shadows/shadow.json", new JObject {
                { "test_shadow", new JObject {
                    { "regular_sprite_name", "test_sprite" },
                    { "location", "images/sprite.png" },
                    { "is_animated", false },
                    { "origin_x", 1 },
                    { "origin_y", 2 },
                    { "margin_left", 3 },
                    { "margin_right", 4 },
                    { "margin_top", 5 },
                    { "margin_bottom", 6 }
                }}
            }.ToString() },
            { "images/sprite.png", "" },
        });
        
        var generatedInformation = _installer.InstallMods([mod], _fileModifier);
        
        Assert.That(generatedInformation.Sprites, Contains.Key(mod.GetId()));
        Assert.That(
            generatedInformation.Sprites[mod.GetId()],
            Has.Some.Matches((SpriteData sprite) =>
                sprite is
                {
                    Name: "test_shadow",
                    Location: "images/sprite.png",
                    IsAnimated: false,
                    OriginX: 1,
                    OriginY: 2,
                    MarginLeft: 3,
                    MarginRight: 4,
                    MarginTop: 5,
                    MarginBottom: 6
                }
            )
        );
    }

    [Test]
    public void ShouldSupportLegacySpriteKey()
    {
        var mod = new MockMod(new Dictionary<string, string> {
            { "shadows/shadow.json", new JObject {
                { "test_shadow", new JObject {
                    { "regular_sprite_name", "test_sprite" },
                    { "sprite", "images/sprite.png" },
                    { "is_animated", false }
                }}
            }.ToString() },
            { "images/sprite.png", "" },
        });
        
        var generatedInformation = _installer.InstallMods([mod], _fileModifier);
        
        Assert.That(generatedInformation.Sprites, Contains.Key(mod.GetId()));
        Assert.That(
            generatedInformation.Sprites[mod.GetId()], 
            Has.Some.Matches((SpriteData sprite) => 
                sprite.Name == "test_shadow" &&
                sprite.Location == "images/sprite.png" &&
                !sprite.IsAnimated
            )
        );
    }
}