using Garethp.ModsOfMistriaInstallerLib.Models;
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
                "animations/Player/Skirts/spr_player_lryn_celine_summer_skirt_waist.png",
                File.ReadAllText(FixtureHandler.GetFixturePath("OutfitMod/spr_player_lryn_celine_summer_skirt_waist.png"))
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

    [Test]
    public void ShouldNotOverrideExistingFilesWhenGenerating()
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
                "animations/Player/Skirts/spr_player_lryn_celine_summer_skirt_waist.meta.toml",
                """
                [asset_properties]
                atlas = "Modded"
                """
            }
        });
        
        new MockInstaller().InstallMod(mod, fileModifier);

        Assert.That(fileModifier.Exists("assets/animations/Player/Skirts/spr_player_lryn_celine_summer_skirt_waist.meta.toml"));
        var skirt = TomlSerializer.Deserialize<SpriteMetaFile>(fileModifier.GetFile(
            "assets/animations/Player/Skirts/spr_player_lryn_celine_summer_skirt_waist.meta.toml"))!;
        Assert.That(skirt.Asset.Atlas, Is.EqualTo("Modded"));
    }

    [Test]
    public void ShouldIncludeLutIfAvailable()
    {
        var fileModifier = new MockFileModifier(new());

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
                "animations/Player/Skirts/spr_player_lryn_celine_summer_skirt_waist.png",
                File.ReadAllText(
                    FixtureHandler.GetFixturePath("OutfitMod/spr_player_lryn_celine_summer_skirt_waist.png"))
            },
            {
                "animations/Player/Skirts/spr_player_lryn_celine_summer_skirt_lut.png",
                File.ReadAllText(FixtureHandler.GetFixturePath("OutfitMod/spr_player_lryn_celine_summer_skirt_lut.png"))
            }
        });

        new MockInstaller().InstallMod(mod, fileModifier);

        Assert.That(
            fileModifier.Exists("assets/animations/Player/Skirts/spr_player_lryn_celine_summer_skirt_lut.meta.toml"),
            Is.True);
        Assert.That(
            fileModifier.Exists("assets/shapes/Player/Skirts/poly_player_lryn_celine_summer_skirt_lut.meta.toml"),
            Is.True);

        var lutMeta = TomlSerializer.Deserialize<TomlTable>(
            fileModifier.GetFile("assets/animations/Player/Skirts/spr_player_lryn_celine_summer_skirt_lut.meta.toml"));
        
        Assert.That(lutMeta, new ContainsTomlConstraint(new TomlTable
        {
            { "asset_properties", new TomlTable
            {
                { "atlas", "Default" },
                { "frame_size", new TomlArray { (long) 5, (long) 256 } },
                { "offset", new TomlTable
                {
                    { "horizontal", "Middle" },
                    { "vertical", "Middle" }
                } }
            }}
        }));
}

    [Test]
    public void ShouldNotIncludeLutIfMissing()
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
                "animations/Player/Skirts/spr_player_lryn_celine_summer_skirt_waist.png",
                File.ReadAllText(FixtureHandler.GetFixturePath("OutfitMod/spr_player_lryn_celine_summer_skirt_waist.png"))
            }
        });
        
        new MockInstaller().InstallMod(mod, fileModifier);
        
        Assert.That(fileModifier.Exists("assets/animations/Player/Skirts/spr_player_lryn_celine_summer_skirt_lut.meta.toml"), Is.False);
        Assert.That(fileModifier.Exists("assets/shapes/Player/Skirts/poly_player_lryn_celine_summer_skirt_lut.meta.toml"), Is.False);
    }

    [Test]
    public void ShouldHandleComplexIcons()
    {
        var fileModifier = new MockFileModifier(new ());
        
        var mod = new MockMod(new Dictionary<string, string>
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
        
        var mod = new MockMod(new Dictionary<string, string>
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
                File.ReadAllText(FixtureHandler.GetFixturePath("OutfitMod/spr_player_lryn_celine_summer_skirt_waist.png"))
            },
            {
                "animations/Player/Head Accessory/spr_player_lryn_celine_summer_hair_head_gear_back.png",
                File.ReadAllText(FixtureHandler.GetFixturePath("OutfitMod/spr_player_lryn_celine_summer_skirt_waist.png"))
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