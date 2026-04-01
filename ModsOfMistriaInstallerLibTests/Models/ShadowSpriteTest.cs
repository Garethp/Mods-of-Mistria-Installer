using Garethp.ModsOfMistriaInstallerLib.Generator;
using Garethp.ModsOfMistriaInstallerLib.Lang;
using Garethp.ModsOfMistriaInstallerLib.Models;
using Garethp.ModsOfMistriaInstallerLib.ModTypes;
using ModsOfMistriaInstallerLibTests.Fixtures;

namespace ModsOfMistriaInstallerLibTests.Models;

[TestFixture]
public class ShadowSpriteTest
{
    private IMod _mockMod;

    [SetUp]
    public void SetUp()
    {
        _mockMod = new MockMod([
            "images/sprite.png",
            "images/animation/1.png",
            "images/empty"
        ]);
    }

    private static ShadowSprite GetMockShadowSprite()
    {
        var sprite = new ShadowSprite
        {
            RegularSpriteName = "test_sprite",
            Sprite = "images/sprite.png",
            IsAnimated = false
        };

        return sprite;
    }

    [Test]
    public void ShouldHaveNoErrorsForValidShadowSpriteWithNoAnimation()
    {
        var shadowSprite = GetMockShadowSprite();
        var validation = shadowSprite.Validate(new Validation(), _mockMod, "shadow.json", "shadow");

        var expectedValidation = new Validation();

        Assert.That(validation, Is.EqualTo(expectedValidation).Using(new ValidationComparer()));
    }
    
    [Test]
    public void ShouldHaveNoErrorsForValidShadowSpriteWithAnimation()
    {
        var shadowSprite = GetMockShadowSprite();
        shadowSprite.Sprite = "images/animation";
        shadowSprite.IsAnimated = true;
        
        var validation = shadowSprite.Validate(new Validation(), _mockMod, "shadow.json", "shadow");

        var expectedValidation = new Validation();

        Assert.That(validation, Is.EqualTo(expectedValidation).Using(new ValidationComparer()));
    }

    [Test]
    public void ShouldHaveErrorForEmptyRegularSpriteName()
    {
        var shadowSprite = GetMockShadowSprite();
        shadowSprite.RegularSpriteName = "";
        var validation = shadowSprite.Validate(new Validation(), _mockMod, "shadow.json", "shadow");

        var expectedValidation = new Validation();
        expectedValidation.AddError(
            _mockMod, 
            "shadow.json",
            string.Format(Resources.CoreErrorShadowHasNoSprite, "shadow")
        );

        Assert.That(validation, Is.EqualTo(expectedValidation).Using(new ValidationComparer()));
    }
    
    [Test]
    public void ShouldHaveErrorForEmptyNonAnimatedSprite()
    {
        var shadowSprite = GetMockShadowSprite();
        shadowSprite.Sprite = "";
        var validation = shadowSprite.Validate(new Validation(), _mockMod, "shadow.json", "shadow");

        var expectedValidation = new Validation();
        expectedValidation.AddError(
            _mockMod,
            "shadow.json",
            string.Format(Resources.CoreErrorShadowHasNoLocation, "shadow")
        );
        
        expectedValidation.AddError(
            _mockMod, 
            "shadow.json",
            string.Format(Resources.CoreItemDoesNotHaveValue, "Shadow shadow")
        );

        Assert.That(validation, Is.EqualTo(expectedValidation).Using(new ValidationComparer()));
    }
    
    [Test]
    public void ShouldHaveErrorForMissingNonAnimatedSprite()
    {
        var shadowSprite = GetMockShadowSprite();
        shadowSprite.Sprite = "images/not-found.png";
        var validation = shadowSprite.Validate(new Validation(), _mockMod, "shadow.json", "shadow");

        var expectedValidation = new Validation();
        expectedValidation.AddError(
            _mockMod, 
            "shadow.json",
            string.Format(Resources.CoreSpriteFileDoesNotExist, "Shadow shadow", "images/not-found.png")
        );

        Assert.That(validation, Is.EqualTo(expectedValidation).Using(new ValidationComparer()));
    }
    
    [Test]
    public void ShouldHaveErrorForEmptyAnimatedSprite()
    {
        var shadowSprite = GetMockShadowSprite();
        shadowSprite.Sprite = "";
        shadowSprite.IsAnimated = true;
        var validation = shadowSprite.Validate(new Validation(), _mockMod, "shadow.json", "shadow");

        var expectedValidation = new Validation();
        expectedValidation.AddError(
            _mockMod,
            "shadow.json",
            string.Format(Resources.CoreErrorShadowHasNoLocation, "shadow")
        );
        
        expectedValidation.AddError(
            _mockMod, 
            "shadow.json",
            string.Format(Resources.CoreItemDoesNotHaveValue, "Shadow shadow")
        );

        Assert.That(validation, Is.EqualTo(expectedValidation).Using(new ValidationComparer()));
    }
    
    [Test]
    public void ShouldHaveErrorForMissingAnimatedSprite()
    {
        var shadowSprite = GetMockShadowSprite();
        shadowSprite.Sprite = "images/not-found";
        shadowSprite.IsAnimated = true;
        var validation = shadowSprite.Validate(new Validation(), _mockMod, "shadow.json", "shadow");

        var expectedValidation = new Validation();
        expectedValidation.AddError(
            _mockMod, 
            "shadow.json",
            string.Format(Resources.CoreSpriteFolderDoesNotExist, "Shadow shadow", "images/not-found")
        );

        Assert.That(validation, Is.EqualTo(expectedValidation).Using(new ValidationComparer()));
    }
    
    [Test]
    public void ShouldHaveErrorForEmptyDirectoryAnimatedSprite()
    {
        var shadowSprite = GetMockShadowSprite();
        shadowSprite.Sprite = "images/empty";
        shadowSprite.IsAnimated = true;
        var validation = shadowSprite.Validate(new Validation(), _mockMod, "shadow.json", "shadow");

        var expectedValidation = new Validation();
        expectedValidation.AddError(
            _mockMod, 
            "shadow.json",
            string.Format(Resources.CoreSpriteFolderIsEmpty, "Shadow shadow", "images/empty")
        );

        Assert.That(validation, Is.EqualTo(expectedValidation).Using(new ValidationComparer()));
    }
}