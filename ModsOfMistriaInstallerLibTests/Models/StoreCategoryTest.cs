using Garethp.ModsOfMistriaInstallerLib.Generator;
using Garethp.ModsOfMistriaInstallerLib.Lang;
using Garethp.ModsOfMistriaInstallerLib.Models;
using Garethp.ModsOfMistriaInstallerLib.ModTypes;
using ModsOfMistriaInstallerLibTests.Fixtures;

namespace ModsOfMistriaInstallerLibTests.Models;

[TestFixture]
public class StoreCategoryTest
{
    private IMod _mockMod;

    [SetUp]
    public void SetUp()
    {
        _mockMod = new MockMod([
            "images/sprite.png"
        ]);
    }

    private static StoreCategory GetMockItem()
    {
        var category = new StoreCategory()
        {
            Store = "general store",
            IconName = "general icon",
            Sprite = "images/sprite.png",
        };

        return category;
    }
    
    [Test]
    public void ShouldHaveNoErrorsForValidStoreCategory()
    {
        var category = GetMockItem();
        var validation = category.Validate(new Validation(), _mockMod, "storeCategory.json");

        var expectedValidation = new Validation();

        Assert.That(validation, Is.EqualTo(expectedValidation).Using(new ValidationComparer()));
    }
    
    [Test]
    public void ShouldHaveErrorIfStoreIsEmpty()
    {
        var category = GetMockItem();
        category.Store = string.Empty;
        var validation = category.Validate(new Validation(), _mockMod, "storeCategory.json");

        var expectedValidation = new Validation();
        expectedValidation.AddError(_mockMod, "storeCategory.json", Resources.CoreErrorStoreCategoryHasNoStore);

        Assert.That(validation, Is.EqualTo(expectedValidation).Using(new ValidationComparer()));
    }
    
    [Test]
    public void ShouldHaveErrorIfIconNameIsEmpty()
    {
        var category = GetMockItem();
        category.IconName = string.Empty;
        var validation = category.Validate(new Validation(), _mockMod, "storeCategory.json");

        var expectedValidation = new Validation();
        expectedValidation.AddError(_mockMod, "storeCategory.json", Resources.CoreErrorStoreCategoryNoName);

        Assert.That(validation, Is.EqualTo(expectedValidation).Using(new ValidationComparer()));
    }
    
    [Test]
    public void ShouldHaveErrorIfSpriteIsEmpty()
    {
        var category = GetMockItem();
        category.Sprite = string.Empty;
        var validation = category.Validate(new Validation(), _mockMod, "storeCategory.json");

        var expectedValidation = new Validation();
        expectedValidation.AddError(_mockMod, "storeCategory.json", string.Format(Resources.CoreItemDoesNotHaveValue, "Category's sprite"));

        Assert.That(validation, Is.EqualTo(expectedValidation).Using(new ValidationComparer()));
    }
    
    [Test]
    public void ShouldHaveErrorIfSpriteDoesNotExist()
    {
        var category = GetMockItem();
        category.Sprite = "image/not-found.png";
        var validation = category.Validate(new Validation(), _mockMod, "storeCategory.json");

        var expectedValidation = new Validation();
        expectedValidation.AddError(_mockMod, "storeCategory.json", string.Format(Resources.CoreSpriteFileDoesNotExist, "Category's sprite", "image/not-found.png"));


        Assert.That(validation, Is.EqualTo(expectedValidation).Using(new ValidationComparer()));
    }
}