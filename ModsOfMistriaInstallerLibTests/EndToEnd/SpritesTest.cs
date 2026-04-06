using Garethp.ModsOfMistriaInstallerLib.Generator;
using Garethp.ModsOfMistriaInstallerLib.Installer;
using Garethp.ModsOfMistriaInstallerLib.Installer.UMT;
using ModsOfMistriaInstallerLibTests.Fixtures;
using ModsOfMistriaInstallerLibTests.TestUtils;
using Newtonsoft.Json.Linq;

namespace ModsOfMistriaInstallerLibTests.EndToEnd;

public class SpritesTest
{
    private MockMod _mockMod;
    
    private MockFileModifier _fileModifier;

    private readonly MockInstaller _installer = new(
        [new SpriteGenerator()], 
        [new OutlineInstaller()]
        );

    [SetUp]
    public void SetUp()
    {
        _fileModifier = new MockFileModifier(new Dictionary<string, string> {
            { "outlines.json", new JObject().ToString() }
        });
        
        _mockMod = GetMod(new JObject
        {
            { "test_sprite", new JObject
            {
                { "Location", "images/sprite.png" },
                { "IsAnimated", false },
            }}
        });
    }

    private MockMod GetMod(JObject contents)
    {
        return new(new Dictionary<string, string>
        {
            { "sprites/test.json", contents.ToString() },
            { "images/sprite.png", "" },
            { "images/outline.png", "" },
            { "images/animated/1.png", "" }
        });
    }

    [Test]
    public void ShouldAddSpriteData()
    {
        var generatedInformation = _installer.InstallMods([_mockMod], _fileModifier);

        Assert.That(generatedInformation.Sprites, Contains.Key(_mockMod.GetId()));
        Assert.That(
            generatedInformation.Sprites[_mockMod.GetId()], 
            Has.Some.Matches((SpriteData sprite) => 
                sprite.Name == "test_sprite" &&
                sprite.Location == "images/sprite.png" &&
                !sprite.IsAnimated &&
                sprite.Mod.GetId() == _mockMod.GetId()
            )
        );
    }
    
    [Test]
    public void ShouldAllowOutlineSprites()
    {
        _mockMod = GetMod(new JObject
        {
            { "test_sprite", new JObject
            {
                { "OutlineLocation", "images/outline.png" },
                { "Location", "images/sprite.png" },
                { "IsAnimated", false },
            }}
        });
        
        var generatedInformation = _installer.InstallMods([_mockMod], _fileModifier);
        
        Assert.That(generatedInformation.Sprites, Contains.Key(_mockMod.GetId()));
        Assert.That(
            generatedInformation.Sprites[_mockMod.GetId()], 
            Has.Some.Matches((SpriteData sprite) => 
                sprite.Name == "test_sprite_outline" &&
                sprite.Location == "images/outline.png" &&
                !sprite.IsAnimated &&
                sprite.Mod.GetId() == _mockMod.GetId()
            )
        );
        
        Assert.That(_fileModifier.GetFile("outlines.json"), new MatchesJsonConstraint(new JObject
        {
            { "test_sprite", "test_sprite_outline" }
        }));
    }

    [Test]
    public void ShouldAllowAnimatedSprite()
    {
        _mockMod = GetMod(new JObject
        {
            { "test_sprite", new JObject
            {
                { "OutlineLocation", "images/outline.png" },
                { "Location", "images/animated" },
                { "IsAnimated", true },
            }}
        });
        
        var generatedInformation = _installer.InstallMods([_mockMod], _fileModifier);
        
        Assert.That(generatedInformation.Sprites, Contains.Key(_mockMod.GetId()));
        
        Assert.That(
            generatedInformation.Sprites[_mockMod.GetId()], 
            Has.Some.Matches((SpriteData sprite) => 
                sprite.Name == "test_sprite" &&
                sprite.Location == "images/animated" &&
                sprite.IsAnimated &&
                sprite.Mod.GetId() == _mockMod.GetId()
            )
        );
        
        Assert.That(
            generatedInformation.Sprites[_mockMod.GetId()], 
            Has.Some.Matches((SpriteData sprite) => 
                sprite.Name == "test_sprite_outline" &&
                sprite.Location == "images/outline.png" &&
                !sprite.IsAnimated &&
                sprite.Mod.GetId() == _mockMod.GetId()
            )
        );
    }

    [Test]
    public void ShouldHaveDefaultValues()
    {
        _mockMod = GetMod(new JObject
        {
            { "test_sprite", new JObject
            {
                { "OutlineLocation", "images/outline.png" },
                { "Location", "images/sprite.png" },
                { "IsAnimated", false },
            }}
        });
        
        var generatedInformation = _installer.InstallMods([_mockMod], _fileModifier);
        
        Assert.That(generatedInformation.Sprites, Contains.Key(_mockMod.GetId()));
        
        Assert.That(
            generatedInformation.Sprites[_mockMod.GetId()], 
            Has.Some.Matches((SpriteData sprite) => 
                sprite.Mod.GetId() == _mockMod.GetId() &&
                sprite is
                {
                    Name: "test_sprite", 
                    Location: "images/sprite.png", 
                    IsAnimated: false, 
                    OriginX: null,
                    OriginY: null,
                    MarginRight: null,
                    MarginLeft: null,
                    MarginTop: null,
                    MarginBottom: null,
                    BoundingBoxMode: null,
                    IsPlayerSprite: false,
                    IsUiSprite: false
                }
            )
        );
        
        Assert.That(
            generatedInformation.Sprites[_mockMod.GetId()], 
            Has.Some.Matches((SpriteData sprite) => 
                sprite.Mod.GetId() == _mockMod.GetId() &&
                sprite is {
                    Name: "test_sprite_outline", 
                    Location: "images/outline.png", 
                    IsAnimated: false, 
                    OriginX: null,
                    OriginY: null,
                    MarginRight: null,
                    MarginLeft: null,
                    MarginTop: null,
                    MarginBottom: null,
                    BoundingBoxMode: null,
                    IsPlayerSprite: false,
                    IsUiSprite: true
                }
            )
        );
    }
    
    [Test]
    public void ShouldAllowModifyingAllSpriteData()
    {
        _mockMod = GetMod(new JObject
        {
            { "test_sprite", new JObject
            {
                { "OutlineLocation", "images/outline.png" },
                { "Location", "images/animated" },
                { "IsAnimated", true },
                { "OriginX", 1 },
                { "OriginY" ,2 },
                { "MarginRight", 3 },
                { "MarginLeft", 4 },
                { "MarginTop", 5 },
                { "MarginBottom", 6 },
                { "BoundingBoxMode", 7 },
                { "IsPlayerSprite", true },
                { "IsUiSprite", true }
            }}
        });
        
        var generatedInformation = _installer.InstallMods([_mockMod], _fileModifier);
        
        Assert.That(generatedInformation.Sprites, Contains.Key(_mockMod.GetId()));
        
        Assert.That(
            generatedInformation.Sprites[_mockMod.GetId()], 
            Has.Some.Matches((SpriteData sprite) => 
                sprite.Mod.GetId() == _mockMod.GetId() &&
                sprite is
                {
                    Name: "test_sprite", 
                    Location: "images/animated", 
                    IsAnimated: true, 
                    OriginX: 1,
                    OriginY: 2,
                    MarginRight: 3,
                    MarginLeft: 4,
                    MarginTop: 5,
                    MarginBottom: 6,
                    BoundingBoxMode: 7,
                    IsPlayerSprite: true,
                    IsUiSprite: true
                }
            )
        );
        
        Assert.That(
            generatedInformation.Sprites[_mockMod.GetId()], 
            Has.Some.Matches((SpriteData sprite) => 
                sprite.Mod.GetId() == _mockMod.GetId() &&
                sprite is {
                    Name: "test_sprite_outline", 
                    Location: "images/outline.png", 
                    IsAnimated: false, 
                    OriginX: 1,
                    OriginY: 2,
                    MarginRight: 3,
                    MarginLeft: 4,
                    MarginTop: 5,
                    MarginBottom: 6,
                    BoundingBoxMode: 7,
                    IsPlayerSprite: false,
                    IsUiSprite: true
                }
            )
        );
    }
}