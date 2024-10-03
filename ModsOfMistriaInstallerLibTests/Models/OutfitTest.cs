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
    public void ShouldValidateLutFileIsNotEmpty()
    {
        var outfit = GetMockOutfit();
        outfit.LutFile = "";
        var validation = outfit.Validate(new Validation(), _mockMod, "outfit.json", "outfit");

        var expectedValidation = new Validation();
        expectedValidation.AddError(
            _mockMod,
            "outfit.json",
            string.Format(
                Resources.ItemDoesNotHaveValue,
                "Outfit outfit's lutFile"
            )
        );

        Assert.That(validation, Is.EqualTo(expectedValidation).Using(new ValidationComparer()));
    }
    
    [Test]
    public void ShouldValidateLutFileExists()
    {
        var outfit = GetMockOutfit();
        outfit.LutFile = "images/not-found.png";
        var validation = outfit.Validate(new Validation(), _mockMod, "outfit.json", "outfit");

        var expectedValidation = new Validation();
        expectedValidation.AddError(
            _mockMod,
            "outfit.json",
            string.Format(
                Resources.SpriteFileDoesNotExist,
                "Outfit outfit's lutFile",
                "images/not-found.png"
            )
        );

        Assert.That(validation, Is.EqualTo(expectedValidation).Using(new ValidationComparer()));
    }
    
    [Test]
    public void ShouldValidateLutFileIsNotDirectory()
    {
        var outfit = GetMockOutfit();
        outfit.LutFile = "images/animation";
        var validation = outfit.Validate(new Validation(), _mockMod, "outfit.json", "outfit");

        var expectedValidation = new Validation();
        expectedValidation.AddError(
            _mockMod,
            "outfit.json",
            string.Format(
                Resources.SpriteFileDoesNotExist,
                "Outfit outfit's lutFile",
                "images/animation"
            )
        );

        Assert.That(validation, Is.EqualTo(expectedValidation).Using(new ValidationComparer()));
    }
    
    [Test]
    public void ShouldValidateUiItemIsNotEmpty()
    {
        var outfit = GetMockOutfit();
        outfit.UiItem = "";
        var validation = outfit.Validate(new Validation(), _mockMod, "outfit.json", "outfit");

        var expectedValidation = new Validation();
        expectedValidation.AddError(
            _mockMod,
            "outfit.json",
            string.Format(
                Resources.ItemDoesNotHaveValue,
                "Outfit outfit's uiItem"
            )
        );

        Assert.That(validation, Is.EqualTo(expectedValidation).Using(new ValidationComparer()));
    }
    
    [Test]
    public void ShouldValidateUiItemExists()
    {
        var outfit = GetMockOutfit();
        outfit.UiItem = "images/not-found.png";
        var validation = outfit.Validate(new Validation(), _mockMod, "outfit.json", "outfit");

        var expectedValidation = new Validation();
        expectedValidation.AddError(
            _mockMod,
            "outfit.json",
            string.Format(
                Resources.SpriteFileDoesNotExist,
                "Outfit outfit's uiItem",
                "images/not-found.png"
            )
        );

        Assert.That(validation, Is.EqualTo(expectedValidation).Using(new ValidationComparer()));
    }
    
    [Test]
    public void ShouldValidateUiItemIsNotDirectory()
    {
        var outfit = GetMockOutfit();
        outfit.UiItem = "images/animation";
        var validation = outfit.Validate(new Validation(), _mockMod, "outfit.json", "outfit");

        var expectedValidation = new Validation();
        expectedValidation.AddError(
            _mockMod,
            "outfit.json",
            string.Format(
                Resources.SpriteFileDoesNotExist,
                "Outfit outfit's uiItem",
                "images/animation"
            )
        );

        Assert.That(validation, Is.EqualTo(expectedValidation).Using(new ValidationComparer()));
    }
    
    [Test]
    public void ShouldValidateOutlineIsNotEmpty()
    {
        var outfit = GetMockOutfit();
        outfit.OutlineFile = "";
        var validation = outfit.Validate(new Validation(), _mockMod, "outfit.json", "outfit");

        var expectedValidation = new Validation();
        expectedValidation.AddError(
            _mockMod,
            "outfit.json",
            string.Format(
                Resources.ItemDoesNotHaveValue,
                "Outfit outfit's outlineFile"
            )
        );

        Assert.That(validation, Is.EqualTo(expectedValidation).Using(new ValidationComparer()));
    }
    
    [Test]
    public void ShouldValidateOutlineExists()
    {
        var outfit = GetMockOutfit();
        outfit.OutlineFile = "images/not-found.png";
        var validation = outfit.Validate(new Validation(), _mockMod, "outfit.json", "outfit");

        var expectedValidation = new Validation();
        expectedValidation.AddError(
            _mockMod,
            "outfit.json",
            string.Format(
                Resources.SpriteFileDoesNotExist,
                "Outfit outfit's outlineFile",
                "images/not-found.png"
            )
        );

        Assert.That(validation, Is.EqualTo(expectedValidation).Using(new ValidationComparer()));
    }
    
    [Test]
    public void ShouldValidateOutlineIsNotDirectory()
    {
        var outfit = GetMockOutfit();
        outfit.OutlineFile = "images/animation";
        var validation = outfit.Validate(new Validation(), _mockMod, "outfit.json", "outfit");

        var expectedValidation = new Validation();
        expectedValidation.AddError(
            _mockMod,
            "outfit.json",
            string.Format(
                Resources.SpriteFileDoesNotExist,
                "Outfit outfit's outlineFile",
                "images/animation"
            )
        );

        Assert.That(validation, Is.EqualTo(expectedValidation).Using(new ValidationComparer()));
    }

    [Test]
    public void ShouldValidateAnimationFileIsNotEmpty()
    {
        var outfit = GetMockOutfit();
        outfit.AnimationFiles = new Dictionary<string, string>();
        var validation = outfit.Validate(new Validation(), _mockMod, "outfit.json", "outfit");

        var expectedValidation = new Validation();
        expectedValidation.AddError(
            _mockMod,
            "outfit.json",
            string.Format(
                Resources.ErrorOutfitNoAnimation,
                "outfit"
            )
        );

        Assert.That(validation, Is.EqualTo(expectedValidation).Using(new ValidationComparer()));
    }

    [Test]
    public void ShouldValidationAnimationFileDirectoryExists()
    {
        var outfit = GetMockOutfit();
        outfit.AnimationFiles = new Dictionary<string, string> { { "back", "images/not-found" } };
        var validation = outfit.Validate(new Validation(), _mockMod, "outfit.json", "outfit");

        var expectedValidation = new Validation();
        expectedValidation.AddError(
            _mockMod,
            "outfit.json",
            string.Format(
                Resources.SpriteFolderDoesNotExist,
                "Outfit outfit's animation file back",
                "images/not-found"
            )
        );

        Assert.That(validation, Is.EqualTo(expectedValidation).Using(new ValidationComparer()));
    }
    
    [Test]
    public void ShouldValidationAnimationFileDirectoryIsNotEmpty()
    {
        var outfit = GetMockOutfit();
        outfit.AnimationFiles = new Dictionary<string, string> { { "back", "images/empty" } };
        var validation = outfit.Validate(new Validation(), _mockMod, "outfit.json", "outfit");

        var expectedValidation = new Validation();
        expectedValidation.AddError(
            _mockMod,
            "outfit.json",
            string.Format(
                Resources.SpriteFolderIsEmpty,
                "Outfit outfit's animation file back",
                "images/empty"
            )
        );

        Assert.That(validation, Is.EqualTo(expectedValidation).Using(new ValidationComparer()));
    }
}