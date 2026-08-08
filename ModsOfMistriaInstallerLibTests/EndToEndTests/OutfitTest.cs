using System.Diagnostics.CodeAnalysis;
using Garethp.ModsOfMistriaInstallerLib.Models.SDK;
using ModsOfMistriaInstallerLibTests.Fixtures;
using ModsOfMistriaInstallerLibTests.TestUtils;
using ModsOfMistriaInstallerLibTests.Utils;
using Tomlyn;
using Tomlyn.Model;

namespace ModsOfMistriaInstallerLibTests.EndToEndTests;

[TestFixture]
public class OutfitTest
{
    public MockMod GetSimpleMod()
    {
        return new MockMod(new Dictionary<string, object>
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
                "animations/Player/Skirts/spr_player_lryn_celine_summer_skirt_waist.png",
                FixtureHandler.ReadAllText("OutfitMod/skirt.png")
            },
        });
    }
    
    [Test]
    public void ShouldInstallAnOutfit()
    {
        var fileModifier = new MockFileModifier(new ());
        
        var mod = GetSimpleMod();
        
        new MockInstaller().InstallMod(mod, fileModifier);
        
        // Check that a UI Item .meta.toml and poly file was created
        Assert.That(fileModifier.Exists("assets/animations/Item Icons/Wearable/spr_ui_item_wearable_lryn_celine_summer_skirt.meta.toml"), Is.True);
        Assert.That(
            fileModifier.GetFile("assets/animations/Item Icons/Wearable/spr_ui_item_wearable_lryn_celine_summer_skirt.meta.toml"),
            new ContainsTomlConstraint(FixtureHandler.ReadAllText("OutfitMod/output/ui.meta.toml"))
        );
        
        Assert.That(fileModifier.Exists("assets/shapes/Item Icons/Wearable/poly_ui_item_wearable_lryn_celine_summer_skirt.meta.toml"), Is.True);
        Assert.That(
            fileModifier.GetFile("assets/shapes/Item Icons/Wearable/poly_ui_item_wearable_lryn_celine_summer_skirt.meta.toml"),
            new ContainsTomlConstraint(FixtureHandler.ReadAllText("OutfitMod/output/ui.poly.toml"))    
        );
        
        // Check that a UI Outline .meta.toml and poly file was created
        Assert.That(fileModifier.Exists("assets/animations/Item Icons/Wearable/spr_ui_item_wearable_lryn_celine_summer_skirt_outline.meta.toml"), Is.True);
        Assert.That(
            fileModifier.GetFile("assets/animations/Item Icons/Wearable/spr_ui_item_wearable_lryn_celine_summer_skirt_outline.meta.toml"),
            new ContainsTomlConstraint(FixtureHandler.ReadAllText("OutfitMod/output/outline.meta.toml"))
        );
        
        Assert.That(fileModifier.Exists("assets/shapes/Item Icons/Wearable/poly_ui_item_wearable_lryn_celine_summer_skirt_outline.meta.toml"), Is.True);
        Assert.That(
            fileModifier.GetFile("assets/shapes/Item Icons/Wearable/poly_ui_item_wearable_lryn_celine_summer_skirt_outline.meta.toml"),
            new ContainsTomlConstraint(FixtureHandler.ReadAllText("OutfitMod/output/outline.poly.toml"))
        );
        
        // Check that the Player Item .meta.toml and poly files were created
        Assert.That(fileModifier.Exists("assets/animations/Player/Skirts/spr_player_lryn_celine_summer_skirt_waist.meta.toml"), Is.True);
        Assert.That(
            fileModifier.GetFile("assets/animations/Player/Skirts/spr_player_lryn_celine_summer_skirt_waist.meta.toml"),
            new ContainsTomlConstraint(FixtureHandler.ReadAllText("OutfitMod/output/waist.meta.toml"))
        );
        
        Assert.That(fileModifier.Exists("assets/shapes/Player/Skirts/poly_player_lryn_celine_summer_skirt_waist.meta.toml"), Is.True);
        Assert.That(
            fileModifier.GetFile("assets/shapes/Player/Skirts/poly_player_lryn_celine_summer_skirt_waist.meta.toml"),
            new ContainsTomlConstraint(FixtureHandler.ReadAllText("OutfitMod/output/waist.poly.toml"))
        );
        
        // Check that it was inserted into player_assets
        Assert.That(fileModifier.Exists("assets/fiddle/player_assets.toml"), Is.True);
        Assert.That(
            fileModifier.GetFile("assets/fiddle/player_assets.toml"),
            new ContainsTomlConstraint(FixtureHandler.ReadAllText("OutfitMod/output/player_assets.toml"))
        );
        
        // Check that it was inserted in outlines.json
        Assert.That(fileModifier.Exists("assets/data_files/animation/outlines.json"), Is.True);
        Assert.That(
            fileModifier.GetFile("assets/data_files/animation/outlines.json"),
            new ContainsJsonConstraint(FixtureHandler.ReadAllText("OutfitMod/output/outlines.json"))
        );
        
        // Check that it was inserted into player_asset_parts.json
        Assert.That(fileModifier.Exists("assets/data_files/animation/player_asset_parts.json"), Is.True);
        Assert.That(
            fileModifier.GetFile("assets/data_files/animation/player_asset_parts.json"),
            new ContainsJsonConstraint(FixtureHandler.ReadAllText("OutfitMod/output/player_asset_parts.json"))
        );
    }
    
    [Test]
    public void ShouldNotOverrideExistingFilesWhenGenerating()
    {
        var fileModifier = new MockFileModifier(new ());

        var mod = GetSimpleMod();
        
        mod.SetFile(
            "animations/Player/Skirts/spr_player_lryn_celine_summer_skirt_waist.meta.toml", 
            """
            [asset_properties]
            atlas = "Modded"
            """    
        );
        
        new MockInstaller().InstallMod(mod, fileModifier);

        Assert.That(fileModifier.Exists("assets/animations/Player/Skirts/spr_player_lryn_celine_summer_skirt_waist.meta.toml"));
        Assert.That(
            fileModifier.GetFile("assets/animations/Player/Skirts/spr_player_lryn_celine_summer_skirt_waist.meta.toml"),
            new ContainsTomlConstraint(new TomlTable
            {
                { "asset_properties", new TomlTable
                {
                    { "atlas", "Modded" }
                }}
            })
        );
    }

    [Test]
    public void ShouldIncludeLutIfAvailable()
    {
        var fileModifier = new MockFileModifier(new());

        var mod = GetSimpleMod();
        
        mod.SetFile(
            "animations/Player/Skirts/spr_player_lryn_celine_summer_skirt_lut.png", 
            FixtureHandler.ReadAllText("OutfitMod/lut.png")
        );

        new MockInstaller().InstallMod(mod, fileModifier);

        Assert.That(fileModifier.Exists("assets/animations/Player/Skirts/spr_player_lryn_celine_summer_skirt_lut.meta.toml"), Is.True);
        Assert.That(
            fileModifier.GetFile("assets/animations/Player/Skirts/spr_player_lryn_celine_summer_skirt_lut.meta.toml"),
            new ContainsTomlConstraint(FixtureHandler.ReadAllText("OutfitMod/output/lut.meta.toml"))
        );
        
        Assert.That(fileModifier.Exists("assets/shapes/Player/Skirts/poly_player_lryn_celine_summer_skirt_lut.meta.toml"), Is.True);
        Assert.That(
            fileModifier.GetFile("assets/shapes/Player/Skirts/poly_player_lryn_celine_summer_skirt_lut.meta.toml"),
            new ContainsTomlConstraint(FixtureHandler.ReadAllText("OutfitMod/output/lut.poly.toml"))
        );
    }

    [Test]
    public void ShouldNotIncludeLutIfMissing()
    {
        var fileModifier = new MockFileModifier(new ());

        var mod = GetSimpleMod();
        mod.RemoveFile("animations/Player/Skirts/spr_player_lryn_celine_summer_skirt_lut.png");
        
        new MockInstaller().InstallMod(mod, fileModifier);
        
        Assert.That(fileModifier.Exists("assets/animations/Player/Skirts/spr_player_lryn_celine_summer_skirt_lut.meta.toml"), Is.False);
        Assert.That(fileModifier.Exists("assets/shapes/Player/Skirts/poly_player_lryn_celine_summer_skirt_lut.meta.toml"), Is.False);
    }

    [Test]
    public void ShouldHandleComplexIcons()
    {
        var fileModifier = new MockFileModifier(new ());
        
        var mod = new MockMod(new Dictionary<string, object>
        {
            { 
                "momi/outfit/lryn_celine_outfit.toml",
                """
                [lryn_celine_summer_hair]
                id = "lryn_celine_summer_hair"
                name = "Celine's summer hair"
                ui_slot = "hair"
                ui_sub_category = "skirt"
                """
            },
        });
        
        new MockInstaller().InstallMod(mod, fileModifier);
        
        var expectedIconMeta = new TomlTable
        {
            { "asset_properties", new TomlTable
            {
                { "atlas", "UI" },
                { "frame_size", new TomlArray { (long) 18, (long) 18 } },
                { "offset", new TomlTable
                {
                    { "horizontal", 9.0 },
                    { "vertical", 9.0 }
                }}
            }}
        };
        
        Assert.That(fileModifier.Exists("assets/animations/Item Icons/Wearable/spr_ui_item_wearable_lryn_celine_summer_hair_asset.meta.toml"), Is.True);
        Assert.That(fileModifier.Exists("assets/animations/Item Icons/Wearable/spr_ui_item_wearable_lryn_celine_summer_hair_body.meta.toml"), Is.True);
        Assert.That(fileModifier.Exists("assets/animations/Item Icons/Wearable/spr_ui_item_wearable_lryn_celine_summer_hair_merged.meta.toml"), Is.True);
        Assert.That(fileModifier.Exists("assets/animations/Item Icons/Wearable/spr_ui_item_wearable_lryn_celine_summer_hair_merged_outline.meta.toml"), Is.True);

        Assert.That(
            fileModifier.GetFile("assets/animations/Item Icons/Wearable/spr_ui_item_wearable_lryn_celine_summer_hair_asset.meta.toml"), 
            new ContainsTomlConstraint(expectedIconMeta)
        );
        Assert.That(
            fileModifier.GetFile("assets/animations/Item Icons/Wearable/spr_ui_item_wearable_lryn_celine_summer_hair_body.meta.toml"), 
            new ContainsTomlConstraint(expectedIconMeta)
        );
        Assert.That(
            fileModifier.GetFile("assets/animations/Item Icons/Wearable/spr_ui_item_wearable_lryn_celine_summer_hair_merged.meta.toml"), 
            new ContainsTomlConstraint(expectedIconMeta)
        );
        Assert.That(
            fileModifier.GetFile("assets/animations/Item Icons/Wearable/spr_ui_item_wearable_lryn_celine_summer_hair_merged_outline.meta.toml"), 
            new ContainsTomlConstraint(expectedIconMeta)
        );

        var expectedIconPoly = new TomlTable
        {
            { "asset_properties", new TomlTable
            {
                { "kind", "box" },
                { "offset",  new TomlArray { (long) -9, (long) -9 } },
                { "dimensions", new TomlArray { (long) 18, (long) 18 } }
            }}
        };
        
        Assert.That(fileModifier.Exists("assets/shapes/Item Icons/Wearable/poly_ui_item_wearable_lryn_celine_summer_hair_asset.meta.toml"), Is.True);
        Assert.That(fileModifier.Exists("assets/shapes/Item Icons/Wearable/poly_ui_item_wearable_lryn_celine_summer_hair_body.meta.toml"), Is.True);
        Assert.That(fileModifier.Exists("assets/shapes/Item Icons/Wearable/poly_ui_item_wearable_lryn_celine_summer_hair_merged.meta.toml"), Is.True);
        Assert.That(fileModifier.Exists("assets/shapes/Item Icons/Wearable/poly_ui_item_wearable_lryn_celine_summer_hair_merged_outline.meta.toml"), Is.True);
        
        Assert.That(
            fileModifier.GetFile("assets/shapes/Item Icons/Wearable/poly_ui_item_wearable_lryn_celine_summer_hair_asset.meta.toml"), 
            new ContainsTomlConstraint(expectedIconPoly)
        );
        Assert.That(
            fileModifier.GetFile("assets/shapes/Item Icons/Wearable/poly_ui_item_wearable_lryn_celine_summer_hair_body.meta.toml"), 
            new ContainsTomlConstraint(expectedIconPoly)
        );
        Assert.That(
            fileModifier.GetFile("assets/shapes/Item Icons/Wearable/poly_ui_item_wearable_lryn_celine_summer_hair_merged.meta.toml"), 
            new ContainsTomlConstraint(expectedIconPoly)
        );
        Assert.That(
            fileModifier.GetFile("assets/shapes/Item Icons/Wearable/poly_ui_item_wearable_lryn_celine_summer_hair_merged_outline.meta.toml"), 
            new ContainsTomlConstraint(expectedIconPoly)
        );    }

    [Test]
    public void ShouldHandleMultiPartOutfits()
    {
        var fileModifier = new MockFileModifier(new ());
        
        var mod = new MockMod(new Dictionary<string, object>
        {
            { 
                "momi/outfit/lryn_celine_outfit.toml",
                """
                [lryn_celine_summer_hair]
                id = "lryn_celine_summer_hair"
                name = "Celine's summer hair"
                ui_slot = "head"
                ui_sub_category = "skirt"
                """
            },
            {
                "animations/Player/Head Accessory/spr_player_lryn_celine_summer_hair_head_gear.png",
                FixtureHandler.ReadAllText("OutfitMod/skirt.png")
            },
            {
                "animations/Player/Head Accessory/spr_player_lryn_celine_summer_hair_head_gear_back.png",
                FixtureHandler.ReadAllText("OutfitMod/skirt.png")
            }
        });
        
        new MockInstaller().InstallMod(mod, fileModifier);
        
        var expectedMeta = new TomlTable
        {
            { "asset_properties", new TomlTable
            {
                { "atlas", "Default" },
                { "frame_size", new TomlArray { (long) 32, (long) 32} },
                { "frame_len", (long) 18 },
                { "duration", 0.025 },
                { "offset", new TomlTable
                {
                    { "horizontal", "Left" },
                    { "vertical", "Top" }
                }}
            }}
        };

        var expectedPoly = new TomlTable
        {
            { "asset_properties", new TomlTable
            {
                { "kind", "box" },
                { "offset", new TomlArray { (long) 0, (long) 0 } },
                { "dimensions", new TomlArray { (long) 32, (long) 32 } }
            }}
        };
        
        Assert.That(fileModifier.Exists("assets/animations/Player/Head Accessory/spr_player_lryn_celine_summer_hair_head_gear.meta.toml"));
        Assert.That(fileModifier.Exists("assets/shapes/Player/Head Accessory/poly_player_lryn_celine_summer_hair_head_gear.meta.toml"));
        
        Assert.That(
            fileModifier.GetFile("assets/animations/Player/Head Accessory/spr_player_lryn_celine_summer_hair_head_gear.meta.toml"), 
            new ContainsTomlConstraint(expectedMeta)
        );
        
        Assert.That(
            fileModifier.GetFile("assets/shapes/Player/Head Accessory/poly_player_lryn_celine_summer_hair_head_gear.meta.toml"), 
            new ContainsTomlConstraint(expectedPoly)
        );
        
        Assert.That(fileModifier.Exists("assets/animations/Player/Head Accessory/spr_player_lryn_celine_summer_hair_head_gear_back.meta.toml"));
        Assert.That(fileModifier.Exists("assets/shapes/Player/Head Accessory/poly_player_lryn_celine_summer_hair_head_gear_back.meta.toml"));
        
        Assert.That(
            fileModifier.GetFile("assets/animations/Player/Head Accessory/spr_player_lryn_celine_summer_hair_head_gear_back.meta.toml"), 
            new ContainsTomlConstraint(expectedMeta)
        );
        
        Assert.That(
            fileModifier.GetFile("assets/shapes/Player/Head Accessory/poly_player_lryn_celine_summer_hair_head_gear_back.meta.toml"), 
            new ContainsTomlConstraint(expectedPoly)
        );
    }
}