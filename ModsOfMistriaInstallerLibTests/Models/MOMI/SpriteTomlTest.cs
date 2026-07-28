using Garethp.ModsOfMistriaInstallerLib.Models.MOMI;

namespace ModsOfMistriaInstallerLibTests.Models.MOMI;

[TestFixture]
public class SpriteTomlTest
{
    [Test]
    public void ShouldStripStartingSpr_FromSpriteId()
    {
        var sprite = new SpriteToml
        {
            Id = "spr_test"
        };
        
        Assert.That(sprite.Id, Is.EqualTo("test"));
    }

    [Test]
    public void ShouldStripStartingSlashFromFomLocation()
    {
        var sprite = new SpriteToml
        {
            FoMFolder = "/location"
        };
        
        Assert.That(sprite.FoMFolder, Is.EqualTo("location"));
    }

    [Test]
    public void ShouldStripStartingAssetsFromFomLocation()
    {
        var sprite = new SpriteToml
        {
            FoMFolder = "assets/location"
        };
        
        Assert.That(sprite.FoMFolder, Is.EqualTo("location"));
    }

    [Test]
    public void ShouldStripStartingAnimationsFromFomLocation()
    {
        var sprite = new SpriteToml
        {
            FoMFolder = "animations/location"
        };
        
        Assert.That(sprite.FoMFolder, Is.EqualTo("location"));
    }

    [Test]
    public void ShouldStripAllStartingItemsFromFomLocation()
    {
        var sprite = new SpriteToml
        {
            FoMFolder = "/assets/animations/location"
        };
        
        Assert.That(sprite.FoMFolder, Is.EqualTo("location"));
    }

    [Test]
    public void ShouldConvertForwardSlashesInFomLocation()
    {
        var sprite = new SpriteToml
        {
            FoMFolder = @"fom\location"
        };
        
        Assert.That(sprite.FoMFolder, Is.EqualTo("fom/location"));
    }
}