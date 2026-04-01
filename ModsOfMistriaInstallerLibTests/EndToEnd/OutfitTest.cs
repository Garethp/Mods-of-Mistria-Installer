using Garethp.ModsOfMistriaInstallerLib.Generator;
using Garethp.ModsOfMistriaInstallerLib.Installer;
using Garethp.ModsOfMistriaInstallerLib.Installer.UMT;
using Garethp.ModsOfMistriaInstallerLib.ModTypes;
using ModsOfMistriaInstallerLibTests.Fixtures;
using ModsOfMistriaInstallerLibTests.Utils;
using Newtonsoft.Json.Linq;

namespace ModsOfMistriaInstallerLibTests.EndToEnd;

[TestFixture]
public class OutfitTest
{
    private IMod _mockMod;
    private MockFileModifier _fileModifier;
    
    private readonly MockInstaller _installer = new([
        new OutfitGenerator()
    ], [
        new LocalisationInstaller(),
        new FiddleInstaller(),
        new AssetPartsInstaller(),
        new OutlineInstaller()
    ]);

    [SetUp]
    public void SetUp()
    {
        _fileModifier =  new(new Dictionary<string, string>
        {
            { "localization.json", "{}" },
            { "__fiddle__.json", "{}" },
            { "player_asset_parts.json", "{}" },
            { "outlines.json", "{}" }
        });
        
        _mockMod = new MockMod(new Dictionary<string, string> {
            { "outfits/outfit.json", new JObject {
                { "test_outfit", new JObject {
                    { "name", "Test Outfit" },
                    { "description", "This is the test outfit" },
                    { "default_unlocked", true },
                    { "ui_slot", "back" },
                    { "ui_sub_category", "capes" },
                    { "lutFile", "images/lut.png" },
                    { "uiItem", "images/ui.png" },
                    { "outlineFile", "images/outline.png" },
                    { "animationFiles", new JObject {
                        { "back", "images/animation" }
                    }}
                    
                }}
            }.ToString() },
            { "images/lut.png", "" },
            { "images/ui.png", "" },
            { "images/outline.png", "" },
            { "images/animation/1.png", "" },
            { "images/animation/2.png", "" }
        });
    }
    
    [Test]
    public void ShouldAddAnimationFiles()
    {
        var mod = _mockMod;
        var generatedInformation = _installer.InstallMods([mod], _fileModifier);
        
        Assert.That(generatedInformation.Sprites, Contains.Key(mod.GetId()));
        Assert.That(
            generatedInformation.Sprites[mod.GetId()], 
            Has.Some.Matches((SpriteData sprite) => 
                sprite.Name == "spr_player_test_outfit_back" &&
                sprite.Location == "images/animation" &&
                sprite.IsPlayerSprite &&
                !sprite.IsUiSprite &&
                sprite.IsAnimated &&
                sprite.BoundingBoxMode == 1 &&
                sprite.DeleteCollisionMask &&
                sprite.SpecialType && 
                sprite.SpecialTypeVersion == 3 &&
                sprite.SpecialPlaybackSpeed == 40 &&
                sprite.Mod.GetId() == mod.GetId()
            )
        );
        
        Assert.That(_fileModifier.GetFile("__fiddle__.json"), new ContainsJsonConstraint(new JObject
        {
            { "player_assets", new JObject
            {
                { "test_outfit", new JObject
                {
                    { "name", "player_assets/test_outfit/name" },
                    { "lut", "spr_player_test_outfit_lut" },
                    { "ui_slot", "back" },
                    { "ui_sub_category", "capes" },
                    { "default_unlocked", true }
                }} 
            }}
        }));
    }
    
    [Test]
    public void ShouldAddUiItemSprite()
    {
        var mod = _mockMod;
        var generatedInformation = _installer.InstallMods([mod], _fileModifier);
        
        Assert.That(generatedInformation.Sprites, Contains.Key(mod.GetId()));
        Assert.That(
            generatedInformation.Sprites[mod.GetId()], 
            Has.Some.Matches((SpriteData sprite) => 
                sprite.Name == "spr_ui_item_wearable_test_outfit" &&
                sprite.Location == "images/ui.png" &&
                !sprite.IsPlayerSprite &&
                sprite.IsUiSprite &&
                !sprite.IsAnimated &&
                sprite.Mod.GetId() == mod.GetId()
            )
        );
    }
    
    [Test]
    public void ShouldAddTheOutfitNameToLocalisation()
    {
        var mod = _mockMod;
        _installer.InstallMods([mod], _fileModifier);
        
        Assert.That(_fileModifier.GetFile("localization.json"), new MatchesJsonConstraint(new JObject
        {
            { "eng", new JObject
            {
                { "player_assets/test_outfit/name", "Test Outfit" }
            }}
        }));
        
        Assert.That(_fileModifier.GetFile("__fiddle__.json"), new ContainsJsonConstraint(new JObject
        {
            { "player_assets", new JObject
            {
                { "test_outfit", new JObject
                {
                    { "name", "player_assets/test_outfit/name" }
                }} 
            }}
        }));
    }
    
    [Test]
    public void ShouldAddAssetParts()
    {
        var mod = _mockMod;
        _installer.InstallMods([mod], _fileModifier);
        
        Assert.That(_fileModifier.GetFile("__fiddle__.json"), new ContainsJsonConstraint(new JObject
        {
            { "player_assets", new JObject
            {
                { "test_outfit", new JObject
                {
                    { "name", "player_assets/test_outfit/name" },
                    { "lut", "spr_player_test_outfit_lut" },
                    { "ui_slot", "back" },
                    { "ui_sub_category", "capes" },
                    { "default_unlocked", true }
                }} 
            }}
        }));
        
        Assert.That(_fileModifier.GetFile("player_asset_parts.json"), new MatchesJsonConstraint(new JObject
        {
            { "test_outfit", new JObject
            {
                { "back", "spr_player_test_outfit_back" }
            }}
        }));
    }

    [Test]
    public void ShouldAddOutline()
    {
        var mod = _mockMod;
        var generatedInformation = _installer.InstallMods([mod], _fileModifier);
        
        Assert.That(generatedInformation.Sprites, Contains.Key(mod.GetId()));
        Assert.That(
            generatedInformation.Sprites[mod.GetId()], 
            Has.Some.Matches((SpriteData sprite) => 
                sprite.Name == "spr_ui_item_wearable_test_outfit_outline" &&
                sprite.Location == "images/outline.png" &&
                !sprite.IsPlayerSprite &&
                sprite.IsUiSprite &&
                !sprite.IsAnimated &&
                sprite.Mod.GetId() == mod.GetId()
            )
        );

        Assert.That(_fileModifier.GetFile("outlines.json"), new MatchesJsonConstraint(new JObject
        {
            { "spr_ui_item_wearable_test_outfit", "spr_ui_item_wearable_test_outfit_outline" }
        }));
    }

    [Test]
    public void ShouldAddLutSprite()
    {
        var mod = _mockMod;
        var generatedInformation = _installer.InstallMods([mod], _fileModifier);
        
        Assert.That(generatedInformation.Sprites, Contains.Key(mod.GetId()));
        Assert.That(
            generatedInformation.Sprites[mod.GetId()], 
            Has.Some.Matches((SpriteData sprite) => 
                sprite.Name == "spr_player_test_outfit_lut" &&
                sprite.Location == "images/lut.png" &&
                sprite.IsPlayerSprite &&
                !sprite.IsUiSprite &&
                !sprite.IsAnimated &&
                sprite.Mod.GetId() == mod.GetId()
            )
        );
    }

    [Test]
    public void ShouldAddMergedOutlineForFaceCosmetics()
    {
        var mod = new MockMod(new Dictionary<string, string> {
            { "outfits/outfit.json", new JObject {
                { "test_outfit", new JObject {
                    { "name", "Test Outfit" },
                    { "description", "This is the test outfit" },
                    { "default_unlocked", true },
                    { "ui_slot", "back" },
                    { "ui_sub_category", "capes" },
                    { "lutFile", "images/lut.png" },
                    { "uiItem", "images/ui.png" },
                    { "outlineFile", "images/outline.png" },
                    { "isFaceCosmetic", true },
                    { "uiAssetFile", "images/ui_asset_file.png" },
                    { "uiBodyFile", "images/ui_body_file.png" },
                    { "animationFiles", new JObject {
                        { "back", "images/animation" }
                    }}
                    
                }}
            }.ToString() },
            { "images/lut.png", "" },
            { "images/ui.png", "" },
            { "images/outline.png", "" },
            { "images/animation/1.png", "" },
            { "images/animation/2.png", "" },
            { "images/ui_asset_file.png", "" },
            { "images/ui_body_file.png", "" }
        });
        
        var generatedInformation = _installer.InstallMods([mod], _fileModifier);
        
        Assert.That(generatedInformation.Sprites, Contains.Key(mod.GetId()));
        
        Assert.That(
            generatedInformation.Sprites[mod.GetId()], 
            Has.Some.Matches((SpriteData sprite) => 
                sprite.Name == "spr_ui_item_wearable_test_outfit_merged" &&
                sprite.Location == "images/ui.png" &&
                !sprite.IsPlayerSprite &&
                sprite.IsUiSprite &&
                !sprite.IsAnimated &&
                sprite.Mod.GetId() == mod.GetId()
            )
        );
        
        Assert.That(
            generatedInformation.Sprites[mod.GetId()], 
            Has.Some.Matches((SpriteData sprite) => 
                sprite.Name == "spr_ui_item_wearable_test_outfit_merged_outline" &&
                sprite.Location == "images/outline.png" &&
                !sprite.IsPlayerSprite &&
                sprite.IsUiSprite &&
                !sprite.IsAnimated &&
                sprite.Mod.GetId() == mod.GetId()
            )
        );
        
        Assert.That(_fileModifier.GetFile("outlines.json"), new MatchesJsonConstraint(new JObject
        {
            { "spr_ui_item_wearable_test_outfit_merged", "spr_ui_item_wearable_test_outfit_merged_outline" }
        }));
    }
    
    [Test]
    public void ShouldAddUiAssetFileSpriteForFaceCosmetics()
    {
        var mod = new MockMod(new Dictionary<string, string> {
            { "outfits/outfit.json", new JObject {
                { "test_outfit", new JObject {
                    { "name", "Test Outfit" },
                    { "description", "This is the test outfit" },
                    { "default_unlocked", true },
                    { "ui_slot", "back" },
                    { "ui_sub_category", "capes" },
                    { "lutFile", "images/lut.png" },
                    { "uiItem", "images/ui.png" },
                    { "outlineFile", "images/outline.png" },
                    { "isFaceCosmetic", true },
                    { "uiAssetFile", "images/ui_asset_file.png" },
                    { "uiBodyFile", "images/ui_body_file.png" },
                    { "animationFiles", new JObject {
                        { "back", "images/animation" }
                    }}
                    
                }}
            }.ToString() },
            { "images/lut.png", "" },
            { "images/ui.png", "" },
            { "images/outline.png", "" },
            { "images/animation/1.png", "" },
            { "images/animation/2.png", "" },
            { "images/ui_asset_file.png", "" },
            { "images/ui_body_file.png", "" }
        });
        
        var generatedInformation = _installer.InstallMods([mod], _fileModifier);
        
        Assert.That(generatedInformation.Sprites, Contains.Key(mod.GetId()));
        
        Assert.That(
            generatedInformation.Sprites[mod.GetId()], 
            Has.Some.Matches((SpriteData sprite) => 
                sprite.Name == "spr_ui_item_wearable_test_outfit_asset" &&
                sprite.Location == "images/ui_asset_file.png" &&
                !sprite.IsPlayerSprite &&
                sprite.IsUiSprite &&
                !sprite.IsAnimated &&
                sprite.Mod.GetId() == mod.GetId()
            )
        ); 
    }

    [Test]
    public void ShouldAddUiBodyFileSpriteForFaceCosmetics()
    {
        var mod = new MockMod(new Dictionary<string, string> {
            { "outfits/outfit.json", new JObject {
                { "test_outfit", new JObject {
                    { "name", "Test Outfit" },
                    { "description", "This is the test outfit" },
                    { "default_unlocked", true },
                    { "ui_slot", "back" },
                    { "ui_sub_category", "capes" },
                    { "lutFile", "images/lut.png" },
                    { "uiItem", "images/ui.png" },
                    { "outlineFile", "images/outline.png" },
                    { "isFaceCosmetic", true },
                    { "uiAssetFile", "images/ui_asset_file.png" },
                    { "uiBodyFile", "images/ui_body_file.png" },
                    { "animationFiles", new JObject {
                        { "back", "images/animation" }
                    }}
                    
                }}
            }.ToString() },
            { "images/lut.png", "" },
            { "images/ui.png", "" },
            { "images/outline.png", "" },
            { "images/animation/1.png", "" },
            { "images/animation/2.png", "" },
            { "images/ui_asset_file.png", "" },
            { "images/ui_body_file.png", "" }
        });
        
        var generatedInformation = _installer.InstallMods([mod], _fileModifier);
        
        Assert.That(generatedInformation.Sprites, Contains.Key(mod.GetId()));
        
        Assert.That(
            generatedInformation.Sprites[mod.GetId()], 
            Has.Some.Matches((SpriteData sprite) => 
                sprite.Name == "spr_ui_item_wearable_test_outfit_body" &&
                sprite.Location == "images/ui_body_file.png" &&
                !sprite.IsPlayerSprite &&
                sprite.IsUiSprite &&
                !sprite.IsAnimated &&
                sprite.Mod.GetId() == mod.GetId()
            )
        ); 
    }
}