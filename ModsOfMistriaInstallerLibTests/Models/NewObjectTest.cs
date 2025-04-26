using Garethp.ModsOfMistriaInstallerLib.Generator;
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
            Prefix = "mod_id",
            Category = "furniture",
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
        expectedValidation.AddError(_mockMod, "new_item.json", string.Format(Resources.ErrorNewObjectNoName));
        
        Assert.That(validation, Is.EqualTo(expectedValidation).Using(new ValidationComparer()));
    }

    [Test]
    public void ShouldAddThePrefixToNameByDefault()
    {
        var newObject = GetNewObject();
        
        Assert.That(newObject.Name, Is.EqualTo("mod_id_new_object"));
    }
    
    [Test]
    public void ShouldAllowDisablingThePrefix()
    {
        var newObject = GetNewObject();
        newObject.DisablePrefix = true;
        
        Assert.That(newObject.Name, Is.EqualTo("new_object"));
    }
    
    [TestCase]
    public void ShouldNotSerializeUnneededProperties()
    {
        var newObject = GetNewObject();
        newObject.DisablePrefix = true;
        var json = JsonConvert.SerializeObject(newObject);
        
        Assert.That(json, Does.Not.Contain("prefix"));
        Assert.That(json, Does.Not.Contain("disable_prefix"));
    }
    
    [Test]
    public void ShouldValidateCategoryIsNotEmpty()
    {
        var newObject = GetNewObject();
        newObject.Category = "";
        var validation = newObject.Validate(new Validation(), _mockMod, "new_item.json", "id");

        var expectedValidation = new Validation();
        expectedValidation.AddError(_mockMod, "new_item.json", string.Format(Resources.ErrorNewObjectNoCategory, "new_object"));
        
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
        expectedValidation.AddError(_mockMod, "new_item.json", string.Format(Resources.ErrorNewObjectInvalidCategory, "new_object", "invalid"));
        
        Assert.That(validation, Is.EqualTo(expectedValidation).Using(new ValidationComparer()));
    }
    
    [Test]
    public void ShouldValidateDataIsNotEmpty()
    {
        var newObject = GetNewObject();
        newObject.Data = new { };
        var validation = newObject.Validate(new Validation(), _mockMod, "new_item.json", "id");

        var expectedValidation = new Validation();
        expectedValidation.AddError(_mockMod, "new_item.json", string.Format(Resources.ErrorNewObjectNoData, "new_object"));
        
        Assert.That(validation, Is.EqualTo(expectedValidation).Using(new ValidationComparer()));
    }
}