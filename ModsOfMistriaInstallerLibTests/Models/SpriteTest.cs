using Garethp.ModsOfMistriaInstallerLib.Generator;
using Garethp.ModsOfMistriaInstallerLib.Lang;
using Garethp.ModsOfMistriaInstallerLib.Models;
using Garethp.ModsOfMistriaInstallerLib.ModTypes;
using ModsOfMistriaInstallerLibTests.Fixtures;

namespace ModsOfMistriaInstallerLibTests.Models;

[TestFixture]
public class SpriteTest
{
    private IMod _mockMod;

    [SetUp]
    public void SetUp()
    {
        _mockMod = new MockMod([
            "images/sprite.png",
            "images/animated/1.png",
            "images/outline.png",
            "images/empty"
        ]);
    }
    
    private Sprite GetMockSprite()
    {
        return new Sprite()
        {
            Name = "test_sprite",
            Location = "images/sprite.png"
        };
    }
    
    [Test]
    public void ShouldHaveNoErrorsOnValidSprite()
    {
        var sprite = GetMockSprite();
        var validation = sprite.Validate(new Validation(), _mockMod, "sprite.json");

        var expectedValidation = new Validation();

        Assert.That(validation, Is.EqualTo(expectedValidation).Using(new ValidationComparer()));
    }
    
    [Test]
    public void ShouldHaveNoErrorsOnValidSpriteWithOutline()
    {
        var sprite = GetMockSprite();
        sprite.OutlineLocation = "images/outline.png";
        var validation = sprite.Validate(new Validation(), _mockMod, "sprite.json");

        var expectedValidation = new Validation();

        Assert.That(validation, Is.EqualTo(expectedValidation).Using(new ValidationComparer()));
    }

    [Test]
    public void ShouldValidateSpriteIsNotEmpty()
    {
        var sprite = GetMockSprite();
        sprite.Location = "";
        sprite.IsAnimated = false;

        var validation = sprite.Validate(new Validation(), _mockMod, "sprite.json");

        var expectedValidation = new Validation();
        expectedValidation.AddError(
            _mockMod,
            "sprite.json",
            string.Format(
                Resources.CoreItemDoesNotHaveValue,
                $"Sprite {sprite.Name}'s location"
            )
        );
        
        Assert.That(validation, Is.EqualTo(expectedValidation).Using(new ValidationComparer()));
    }
    
    [Test]
    public void ShouldValidateSpriteFileExistsIfNotAnimated()
    {
        var sprite = GetMockSprite();
        sprite.Location = "images/not-found.png";
        sprite.IsAnimated = false;

        var validation = sprite.Validate(new Validation(), _mockMod, "sprite.json");

        var expectedValidation = new Validation();
        expectedValidation.AddError(
            _mockMod,
            "sprite.json",
            string.Format(
                Resources.CoreSpriteFileDoesNotExist,
                $"Sprite {sprite.Name}'s location",
                sprite.Location
            )
        );
        
        Assert.That(validation, Is.EqualTo(expectedValidation).Using(new ValidationComparer()));
    }

    [Test]
    public void ShouldValidateSpriteFolderExistsIfAnimated()
    {
        var sprite = GetMockSprite();
        sprite.Location = "images/not-found";
        sprite.IsAnimated = true;

        var validation = sprite.Validate(new Validation(), _mockMod, "sprite.json");

        var expectedValidation = new Validation();
        expectedValidation.AddError(
            _mockMod,
            "sprite.json",
            string.Format(
                Resources.CoreSpriteFolderDoesNotExist,
                $"Sprite {sprite.Name}'s location",
                sprite.Location
            )
        );
        
        Assert.That(validation, Is.EqualTo(expectedValidation).Using(new ValidationComparer()));
    }
    
    [Test]
    public void ShouldValidateSpriteFolderIsNotEmptyIfAnimated()
    {
        var sprite = GetMockSprite();
        sprite.Location = "images/empty";
        sprite.IsAnimated = true;

        var validation = sprite.Validate(new Validation(), _mockMod, "sprite.json");

        var expectedValidation = new Validation();
        expectedValidation.AddError(
            _mockMod,
            "sprite.json",
            string.Format(
                Resources.CoreSpriteFolderIsEmpty,
                $"Sprite {sprite.Name}'s location",
                sprite.Location
            )
        );
        
        Assert.That(validation, Is.EqualTo(expectedValidation).Using(new ValidationComparer()));
    }

    [Test]
    public void ShouldValidateOutlineSpriteIfGiven()
    {
        var sprite = GetMockSprite();
        sprite.OutlineLocation = "images/not-found";

        var validation = sprite.Validate(new Validation(), _mockMod, "sprite.json");

        var expectedValidation = new Validation();
        expectedValidation.AddError(
            _mockMod,
            "sprite.json",
            string.Format(
                Resources.CoreSpriteFileDoesNotExist,
                $"Sprite {sprite.Name}'s outline_location",
                sprite.OutlineLocation
            )
        );
        
        Assert.That(validation, Is.EqualTo(expectedValidation).Using(new ValidationComparer()));
    }
}