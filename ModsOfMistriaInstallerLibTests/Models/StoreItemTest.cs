﻿using Garethp.ModsOfMistriaInstallerLib.Generator;
using Garethp.ModsOfMistriaInstallerLib.Lang;
using Garethp.ModsOfMistriaInstallerLib.Models;
using Garethp.ModsOfMistriaInstallerLib.ModTypes;
using ModsOfMistriaInstallerLibTests.Fixtures;

namespace ModsOfMistriaInstallerLibTests.Models;

[TestFixture]
public class StoreItemTest
{
    private IMod _mockMod;

    [SetUp]
    public void SetUp()
    {
        _mockMod = new MockMod([]);
    }
    
    private static StoreItem GetMockItem()
    {
        var item = new StoreItem()
        {
            Store = "general store",
            Category = "general category",
        };

        return item;
    }
    
    [Test]
    public void ShouldHaveNoErrorsForValidStoreItem()
    {
        var item = GetMockItem();
        var validation = item.Validate(new Validation(), _mockMod, "storeItem.json");

        var expectedValidation = new Validation();

        Assert.That(validation, Is.EqualTo(expectedValidation).Using(new ValidationComparer()));
    }
    
    [TestCase("spring")]
    [TestCase("summer")]
    [TestCase("fall")]
    [TestCase("winter")]
    public void ShouldHaveNoErrorsForValidSeason(string season)
    {
        var item = GetMockItem();
        item.Season = season;
        var validation = item.Validate(new Validation(), _mockMod, "storeItem.json");

        var expectedValidation = new Validation();

        Assert.That(validation, Is.EqualTo(expectedValidation).Using(new ValidationComparer()));
    }

    
    [Test]
    public void ShouldHaveErrorIfNoStoreDefined()
    {
        var item = GetMockItem();
        item.Store = "";
        var validation = item.Validate(new Validation(), _mockMod, "storeItem.json");

        var expectedValidation = new Validation();
        expectedValidation.AddError(_mockMod, "storeItem.json", Resources.ErrorStoreItemHasNoStore);

        Assert.That(validation, Is.EqualTo(expectedValidation).Using(new ValidationComparer()));
    }
    
    [Test]
    public void ShouldHaveErrorIfNoStoreCategoryDefined()
    {
        var item = GetMockItem();
        item.Category = "";
        var validation = item.Validate(new Validation(), _mockMod, "storeItem.json");

        var expectedValidation = new Validation();
        expectedValidation.AddError(_mockMod, "storeItem.json", string.Format(Resources.ErrorStoreItemHasNoCategory, "general store"));

        Assert.That(validation, Is.EqualTo(expectedValidation).Using(new ValidationComparer()));
    }

    [Test]
    public void ShouldHaveErrorIfSeasonIsIncorrect()
    {
        var item = GetMockItem();
        item.Season = "invalid";
        var validation = item.Validate(new Validation(), _mockMod, "storeItem.json");

        var expectedValidation = new Validation();
        expectedValidation.AddError(_mockMod, "storeItem.json", string.Format(Resources.ErrorStoreItemHasInvalidSeason, item.Store, item.Category, "invalid"));

        Assert.That(validation, Is.EqualTo(expectedValidation).Using(new ValidationComparer()));
    }
}