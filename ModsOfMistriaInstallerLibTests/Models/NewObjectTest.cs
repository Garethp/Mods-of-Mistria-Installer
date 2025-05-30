﻿using Garethp.ModsOfMistriaInstallerLib.Generator;
using Garethp.ModsOfMistriaInstallerLib.Lang;
using Garethp.ModsOfMistriaInstallerLib.Models;
using Garethp.ModsOfMistriaInstallerLib.ModTypes;
using ModsOfMistriaInstallerLibTests.Fixtures;
using Newtonsoft.Json;

namespace ModsOfMistriaInstallerLibTests.Models;

[TestFixture]
public class NewObjectTest
{
    private IMod _mockMod;

    [SetUp]
    public void SetUp()
    {
        _mockMod = new MockMod([
        ]);
    }

    private static NewObject GetNewObject()
    {
        var newObject = new NewObject
        {
            Name = "new_object",
            Category = "furniture",
            OverwritesOtherMod = false,
            Data = new {
                dummy = "data"
            }
        };

        return newObject;
    }

    [Test]
    public void ShouldHaveNoErrorsForValidObject()
    {
        var newObject = GetNewObject();
        var validation = newObject.Validate(new Validation(), _mockMod, "new_item.json", "id");

        var expectedValidation = new Validation();
        
        Assert.That(validation, Is.EqualTo(expectedValidation).Using(new ValidationComparer()));
    }
    
    [Test]
    public void ShouldValidateNameIsNotEmpty()
    {
        var newObject = GetNewObject();
        newObject.Name = "";
        var validation = newObject.Validate(new Validation(), _mockMod, "new_item.json", "id");

        var expectedValidation = new Validation();
        expectedValidation.AddError(_mockMod, "new_item.json", string.Format(Resources.CoreErrorNewObjectNoName));
        
        Assert.That(validation, Is.EqualTo(expectedValidation).Using(new ValidationComparer()));
    }

    [Test]
    public void ShouldValidateOverwritesOtherModIsPresent()
    {
        var newObject = GetNewObject();
        newObject.OverwritesOtherMod = null;
        
        var validation = newObject.Validate(new Validation(), _mockMod, "new_item.json", "id");
        var expectedValidation = new Validation();
        expectedValidation.AddError(_mockMod, "new_item.json", string.Format(Resources.CoreErrorNewObjectHasNoOverwritesOtherMod, newObject.Name));
        
        Assert.That(validation, Is.EqualTo(expectedValidation).Using(new ValidationComparer()));
    }
    
    [Test]
    public void ShouldNotSerializeOverwritesOtherMod()
    {
        var newObject = GetNewObject();
        Assert.That(newObject.ShouldSerializeOverwritesOtherMod(), Is.False);
    }
    
    [Test]
    public void ShouldValidateCategoryIsNotEmpty()
    {
        var newObject = GetNewObject();
        newObject.Category = "";
        var validation = newObject.Validate(new Validation(), _mockMod, "new_item.json", "id");

        var expectedValidation = new Validation();
        expectedValidation.AddError(_mockMod, "new_item.json", string.Format(Resources.CoreErrorNewObjectNoCategory, "new_object"));
        
        Assert.That(validation, Is.EqualTo(expectedValidation).Using(new ValidationComparer()));
    }
    
    [TestCase("breakable")]
    [TestCase("building")]
    [TestCase("bush")]
    [TestCase("crop")]
    [TestCase("dig_site")]
    [TestCase("furniture")]
    [TestCase("grass")]
    [TestCase("rock")]
    [TestCase("stump")]
    [TestCase("tree")]
    public void ShouldValidateCategoryCanAcceptValidCategories(string category)
    {
        var newObject = GetNewObject();
        newObject.Category = category;
        var validation = newObject.Validate(new Validation(), _mockMod, "new_item.json", "id");

        var expectedValidation = new Validation();
        
        Assert.That(validation, Is.EqualTo(expectedValidation).Using(new ValidationComparer()));
    }
    
    [Test]
    public void ShouldValidateCategoryCannotAcceptInvalidCategories()
    {
        var newObject = GetNewObject();
        newObject.Category = "invalid";
        var validation = newObject.Validate(new Validation(), _mockMod, "new_item.json", "id");

        var expectedValidation = new Validation();
        expectedValidation.AddError(_mockMod, "new_item.json", string.Format(Resources.CoreErrorNewObjectInvalidCategory, "new_object", "invalid"));
        
        Assert.That(validation, Is.EqualTo(expectedValidation).Using(new ValidationComparer()));
    }
    
    [Test]
    public void ShouldValidateDataIsNotEmpty()
    {
        var newObject = GetNewObject();
        newObject.Data = new { };
        var validation = newObject.Validate(new Validation(), _mockMod, "new_item.json", "id");

        var expectedValidation = new Validation();
        expectedValidation.AddError(_mockMod, "new_item.json", string.Format(Resources.CoreErrorNewObjectNoData, "new_object"));
        
        Assert.That(validation, Is.EqualTo(expectedValidation).Using(new ValidationComparer()));
    }
}