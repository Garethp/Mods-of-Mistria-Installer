using Garethp.ModsOfMistriaInstallerLib.Generator;
using Garethp.ModsOfMistriaInstallerLib.Lang;
using Garethp.ModsOfMistriaInstallerLib.Models;
using Garethp.ModsOfMistriaInstallerLib.ModTypes;
using ModsOfMistriaInstallerLibTests.Fixtures;

namespace ModsOfMistriaInstallerLibTests.Models;

[TestFixture]
public class OutfitTest
{
    private readonly IMod _mockMod = new MockMod();

    [SetUp]
    public void SetUp()
    {
    }

    private static Outfit GetMockOutfit()
    {
        var outfit = new Outfit();
        outfit.Name = "Outfit Name";
        outfit.Description = "Outfit Description";
        outfit.DefaultUnlocked = false;
        outfit.UiSlot = "back";
        outfit.UiSubCategory = "back";
        outfit.LutFile = "images/lut.png";
        outfit.UiItem = "images/ui.png";
        outfit.OutlineFile = "images/outline.png";
        outfit.AnimationFiles = new Dictionary<string, string>
        {
            { "back", "images/animation" },
        };

        return outfit;
    }

    [Test]
    public void ShouldValidateNameIsNotEmpty()
    {
        var outfit = GetMockOutfit();
        outfit.Name = "";
        var validation = outfit.Validate(new Validation(), _mockMod, "outfit.json", "outfit");

        var expectedValidation = new Validation();
        expectedValidation.AddError(_mockMod, "outfit.json", Resources.ErrorOutfitNoName);

        Assert.That(validation, Is.EqualTo(expectedValidation).Using(new ValidationComparer()));
    }

    [Test]
    public void ShouldValidateUiSlotIsNotEmpty()
    {
        var outfit = GetMockOutfit();
        outfit.UiSlot = "";
        var validation = outfit.Validate(new Validation(), _mockMod, "outfit.json", "outfit");

        var expectedValidation = new Validation();
        expectedValidation.AddError(_mockMod, "outfit.json", string.Format(Resources.ErrorOutfitNoUiSlot, "outfit"));

        Assert.That(validation, Is.EqualTo(expectedValidation).Using(new ValidationComparer()));
    }

    [Test]
    public void ShouldValidateUiSlotHasCorrectValue()
    {
        var outfit = GetMockOutfit();
        outfit.UiSlot = "invalid";
        var validation = outfit.Validate(new Validation(), _mockMod, "outfit.json", "outfit");

        var expectedValidation = new Validation();
        expectedValidation.AddError(
            _mockMod,
            "outfit.json",
            string.Format(
                Resources.ErrorOutfitUiSlotWrong,
                "outfit",
                string.Join(", ", Outfit.ValidSlots.Keys)
            )
        );

        Assert.That(validation, Is.EqualTo(expectedValidation).Using(new ValidationComparer()));
    }

    [Test]
    public void ShouldValidateUiSubCategoryIsNotEmpty()
    {
        var outfit = GetMockOutfit();
        outfit.UiSubCategory = "";
        var validation = outfit.Validate(new Validation(), _mockMod, "outfit.json", "outfit");

        var expectedValidation = new Validation();
        expectedValidation.AddError(
            _mockMod,
            "outfit.json",
            string.Format(
                Resources.ErrorOutfitNoSubCategory,
                "outfit"
            )
        );

        Assert.That(validation, Is.EqualTo(expectedValidation).Using(new ValidationComparer()));
    }
    
    [Test]
    public void ShouldValidateUiSubCategoryHasCorrectValue()
    {
        var outfit = GetMockOutfit();
        outfit.UiSubCategory = "invalid";
        var validation = outfit.Validate(new Validation(), _mockMod, "outfit.json", "outfit");

        var expectedValidation = new Validation();
        expectedValidation.AddError(
            _mockMod,
            "outfit.json",
            string.Format(
                Resources.ErrorOutfitUiSubCategoryWrong,
                "outfit",
                string.Join(", ", Outfit.ValidSlots[outfit.UiSlot])
            )
        );

        Assert.That(validation, Is.EqualTo(expectedValidation).Using(new ValidationComparer()));
    }

    [Test]
    [Ignore("@TODO")]
    public void ShouldValidateLutFileIsNotEmpty()
    {
    }
    
    [Test]
    [Ignore("@TODO")]
    public void ShouldValidateLutFileExists()
    {
    }
    
    [Test]
    [Ignore("@TODO")]
    public void ShouldValidateLutFileIsNotDirectory()
    {
    }
    
    [Test]
    [Ignore("@TODO")]
    public void ShouldValidateUiItemIsNotEmpty()
    {
    }
    
    [Test]
    [Ignore("@TODO")]
    public void ShouldValidateUiItemExists()
    {
    }
    
    [Test]
    [Ignore("@TODO")]
    public void ShouldValidateUiItemIsNotDirectory()
    {
    }
    
    [Test]
    [Ignore("@TODO")]
    public void ShouldValidateOutlineIsNotEmpty()
    {
    }
    
    [Test]
    [Ignore("@TODO")]
    public void ShouldValidateOutlineExists()
    {
    }
    
    [Test]
    [Ignore("@TODO")]
    public void ShouldValidateOutlineIsNotDirectory()
    {
    }

    [Test]
    [Ignore("@TODO")]
    public void ShouldValidateAnimationFileIsNotEmpty()
    {
    }

    [Test]
    [Ignore("@TODO")]
    public void ShouldValidationAnimationFileDirectoryExists()
    {
    }
}