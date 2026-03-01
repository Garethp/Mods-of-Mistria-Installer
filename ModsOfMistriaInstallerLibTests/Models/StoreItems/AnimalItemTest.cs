using Garethp.ModsOfMistriaInstallerLib.Generator;
using Garethp.ModsOfMistriaInstallerLib.Lang;
using Garethp.ModsOfMistriaInstallerLib.Models.StoreItems;
using Garethp.ModsOfMistriaInstallerLib.ModTypes;
using ModsOfMistriaInstallerLibTests.Fixtures;
using Newtonsoft.Json.Linq;

namespace ModsOfMistriaInstallerLibTests.Models.StoreItems;

[TestFixture]
public class AnimalItemTest
{
    private IMod _mockMod;

    [SetUp]
    public void SetUp()
    {
        _mockMod = new MockMod([]);
    }

    private static AnimalStoreItem GetMockItem()
    {
        var item = new AnimalStoreItem()
        {
            Store = "general store",
            Category = "general category",
            Item = new AnimalItemDefinition() { Animal = "test animal", AnimalCosmetic = "test item" }
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
        expectedValidation.AddError(_mockMod, "storeItem.json", Resources.CoreErrorStoreItemHasNoStore);

        Assert.That(validation, Is.EqualTo(expectedValidation).Using(new ValidationComparer()));
    }

    [Test]
    public void ShouldHaveErrorIfNoStoreCategoryDefined()
    {
        var item = GetMockItem();
        item.Category = "";
        var validation = item.Validate(new Validation(), _mockMod, "storeItem.json");

        var expectedValidation = new Validation();
        expectedValidation.AddError(_mockMod, "storeItem.json",
            string.Format(Resources.CoreErrorStoreItemHasNoCategory, "general store"));

        Assert.That(validation, Is.EqualTo(expectedValidation).Using(new ValidationComparer()));
    }

    [Test]
    public void ShouldHaveErrorIfSeasonIsIncorrect()
    {
        var item = GetMockItem();
        item.Season = "invalid";
        var validation = item.Validate(new Validation(), _mockMod, "storeItem.json");

        var expectedValidation = new Validation();
        expectedValidation.AddError(_mockMod, "storeItem.json",
            string.Format(Resources.CoreErrorStoreItemHasInvalidSeason, item.Store, item.Category, "invalid"));

        Assert.That(validation, Is.EqualTo(expectedValidation).Using(new ValidationComparer()));
    }

    [Test]
    public void ShouldHaveErrorIfItemIsEmpty()
    {
        var item = GetMockItem();
        item.Item = null;
        var validation = item.Validate(new Validation(), _mockMod, "storeItem.json");

        var expectedValidation = new Validation();
        expectedValidation.AddError(_mockMod, "storeItem.json",
            string.Format(Resources.CoreErrorStoreItemHasNoItem, item.Store, item.Category));

        Assert.That(validation, Is.EqualTo(expectedValidation).Using(new ValidationComparer()));
    }

    [Test]
    public void ShouldHaveErrorIfItemHasNoAnimal()
    {
        var item = GetMockItem();
        item.Item.Animal = "";
        var validation = item.Validate(new Validation(), _mockMod, "storeItem.json");

        var expectedValidation = new Validation();
        expectedValidation.AddError(_mockMod, "storeItem.json",
            string.Format(Resources.CoreErrorStoreItemHasNoItem, item.Store, item.Category));

        Assert.That(validation, Is.EqualTo(expectedValidation).Using(new ValidationComparer()));
    }

    [Test]
    public void ShouldHaveErrorIfAnimalHasNoCosmetic()
    {
        var item = GetMockItem();
        item.Item.AnimalCosmetic = "";
        var validation = item.Validate(new Validation(), _mockMod, "storeItem.json");

        var expectedValidation = new Validation();
        expectedValidation.AddError(_mockMod, "storeItem.json",
            string.Format(Resources.CoreErrorStoreItemAnimalHasNoCosmetic, item.Item.Animal, item.Store));

        Assert.That(validation, Is.EqualTo(expectedValidation).Using(new ValidationComparer()));
    }
    
    [Test]
    public void ShouldGenerateJsonCorrectly()
    {
        var item = GetMockItem();

        var actualJson = item.ToJson();
        var expectedJson = new JObject
        {
            { "animal", item.Item.Animal },
            { "animal_cosmetic", item.Item.AnimalCosmetic },
        };

        Assert.That(actualJson.ToString(), Is.EqualTo(expectedJson.ToString()));
    }

    [Test]
    public void ShouldGenerateJsonWithRequirements()
    {
        var item = GetMockItem();
        var requirements = new JObject
        {
            { "unlocked_animal", "horse" }
        };

        item.requirements = requirements;

        var actualJson = item.ToJson();
        var expectedJson = new JObject
        {
            { "requirements", requirements },
            { "animal", item.Item.Animal },
            { "animal_cosmetic", item.Item.AnimalCosmetic },
        };

        Assert.That(actualJson.ToString(), Is.EqualTo(expectedJson.ToString()));
    }
}