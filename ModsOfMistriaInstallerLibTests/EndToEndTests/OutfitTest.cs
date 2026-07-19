using Garethp.ModsOfMistriaInstallerLib.Models;
using ModsOfMistriaInstallerLibTests.Fixtures;
using ModsOfMistriaInstallerLibTests.TestUtils;
using ModsOfMistriaInstallerLibTests.Utils;
using Tomlyn;

namespace ModsOfMistriaInstallerLibTests.EndToEndTests;

public class OutfitTest
{
    [Test]
    public void ShouldInstallAnOutfit()
    {
        var fileModifier = new MockFileModifier(new ());
        
        var mod = new MockMod(new Dictionary<string, string>
        {
            { 
                "momi/outfit/lryn_celine_outfit.toml",
                """
                [lryn_celine_summer_skirt]
                id = "lryn_celine_summer_skirt"
                name = "Celine's summer skirt"
                ui_slot = "skirt"
                ui_sub_category = "skirt"
                default_unlocked = true
                """
            },
            {
                "momi/images/spr_player_lryn_celine_summer_skirt_waist.png",
                File.ReadAllText(FixtureHandler.GetFixturePath("OutfitMod/spr_player_lryn_celine_summer_skirt_waist.png"))
            },
            {
                "momi/images/spr_player_lryn_celine_summer_skirt_lut.png",
                File.ReadAllText(FixtureHandler.GetFixturePath("OutfitMod/spr_player_lryn_celine_summer_skirt_lut.png"))
            }
        });
        
        new MockInstaller().InstallMod(mod, fileModifier);
        
        // Check that a UI Item .meta.toml and poly file was created
        Assert.That(fileModifier.Exists("assets/animations/Item Icons/Wearable/spr_ui_item_wearable_lryn_celine_summer_skirt.meta.toml"), Is.True);
        var skirtUiItem = TomlSerializer.Deserialize<SpriteMetaFile>(fileModifier.GetFile(
            "assets/animations/Item Icons/Wearable/spr_ui_item_wearable_lryn_celine_summer_skirt.meta.toml"))!;
        Assert.That(skirtUiItem.Meta!.AssetKind, Is.EqualTo("Animation"));
        Assert.That(skirtUiItem.Asset!.Atlas, Is.EqualTo("UI"));
        
        Assert.That(fileModifier.Exists("assets/shapes/Item Icons/Wearable/poly_ui_item_wearable_lryn_celine_summer_skirt.meta.toml"), Is.True);
        var skirtUiPoly = TomlSerializer.Deserialize<ShapeMeta>(fileModifier.GetFile(
            "assets/shapes/Item Icons/Wearable/poly_ui_item_wearable_lryn_celine_summer_skirt.meta.toml"))!;
        Assert.That(skirtUiPoly.Meta.AssetKind, Is.EqualTo("Shape"));
        Assert.That(skirtUiPoly.Asset.Kind, Is.EqualTo("box"));
        
        // Check that a UI Outline .meta.toml and poly file was created
        Assert.That(fileModifier.Exists("assets/animations/Item Icons/Wearable/spr_ui_item_wearable_lryn_celine_summer_skirt_outline.meta.toml"), Is.True);
        var skirtOutline = TomlSerializer.Deserialize<SpriteMetaFile>(fileModifier.GetFile(
            "assets/animations/Item Icons/Wearable/spr_ui_item_wearable_lryn_celine_summer_skirt_outline.meta.toml"))!;
        Assert.That(skirtOutline.Meta!.AssetKind, Is.EqualTo("Animation"));
        Assert.That(skirtOutline.Asset!.Atlas, Is.EqualTo("UI"));
        
        Assert.That(fileModifier.Exists("assets/shapes/Item Icons/Wearable/poly_ui_item_wearable_lryn_celine_summer_skirt_outline.meta.toml"), Is.True);
        var skirtOutlineShape = TomlSerializer.Deserialize<ShapeMeta>(fileModifier.GetFile(
            "assets/shapes/Item Icons/Wearable/poly_ui_item_wearable_lryn_celine_summer_skirt_outline.meta.toml"))!;
        Assert.That(skirtOutlineShape.Meta.AssetKind, Is.EqualTo("Shape"));
        Assert.That(skirtOutlineShape.Asset.Kind, Is.EqualTo("box"));
        
        // Check that the Player Item .meta.toml and poly files were created
        Assert.That(fileModifier.Exists("assets/animations/Player/Skirts/spr_player_lryn_celine_summer_skirt_waist.meta.toml"), Is.True);
        var skirt = TomlSerializer.Deserialize<SpriteMetaFile>(fileModifier.GetFile(
            "assets/animations/Player/Skirts/spr_player_lryn_celine_summer_skirt_waist.meta.toml"))!;
        Assert.That(skirt.Meta!.AssetKind, Is.EqualTo("Animation"));
        Assert.That(skirt.Asset!.Atlas, Is.EqualTo("Default"));
        
        Assert.That(fileModifier.Exists("assets/shapes/Player/Skirts/poly_player_lryn_celine_summer_skirt_waist.meta.toml"), Is.True);
        var skirtShape = TomlSerializer.Deserialize<ShapeMeta>(fileModifier.GetFile(
            "assets/shapes/Player/Skirts/poly_player_lryn_celine_summer_skirt_waist.meta.toml"))!;
        Assert.That(skirtShape.Meta.AssetKind, Is.EqualTo("Shape"));
        Assert.That(skirtShape.Asset.Kind, Is.EqualTo("box"));
        
        // Check that it was inserted into player_assets
        Assert.That(fileModifier.Exists("assets/fiddle/player_assets.toml"), Is.True);
        
        // Check that it was inserted in outlines.json
        Assert.That(fileModifier.Exists("assets/data_files/animation/outlines.json"), Is.True);
        
        // Check that it was inserted into player_asset_parts.json
        Assert.That(fileModifier.Exists("assets/data_files/animation/player_asset_parts.json"), Is.True);
    }
}