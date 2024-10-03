using Garethp.ModsOfMistriaInstallerLib;
using Garethp.ModsOfMistriaInstallerLib.Lang;
using Garethp.ModsOfMistriaInstallerLib.ModTypes;
using ModsOfMistriaInstallerLibTests.Fixtures;

namespace ModsOfMistriaInstallerLibTests;

[TestFixture]
public class ValidationToolsTest
{
    private IMod _mockMock = new MockMod();
    
    [Test]
    public void ShouldCheckIfSpriteFileIsNotNull()
    {
        var error = ValidationTools.CheckSpriteFileExists(_mockMock, "outfit", null);
        
        Assert.That(error, Is.EqualTo(string.Format(Resources.ItemDoesNotHaveValue, "outfit")));
    }
    
    [Test]
    public void ShouldCheckIfSpriteFileIsNotEmptyString()
    {
        var error = ValidationTools.CheckSpriteFileExists(_mockMock, "outfit", "");
        
        Assert.That(error, Is.EqualTo(string.Format(Resources.ItemDoesNotHaveValue, "outfit")));
    }
    
    [Test]
    public void ShouldCheckIfSpriteFileExists()
    {
        var error = ValidationTools.CheckSpriteFileExists(_mockMock, "outfit", "images/not-found.png");
        
        Assert.That(error, Is.EqualTo(string.Format(Resources.SpriteFileDoesNotExist, "outfit", "images/not-found.png")));
    }
    
    [Test]
    public void ShouldCheckIfSpriteFileExistsAsFile()
    {
        var error = ValidationTools.CheckSpriteFileExists(_mockMock, "outfit", "images/animation");
        
        Assert.That(error, Is.EqualTo(string.Format(Resources.SpriteFileDoesNotExist, "outfit", "images/animation")));
    }

    [Test]
    public void ShouldCheckIfSpriteDirectoryIsNotNull()
    {
        var error = ValidationTools.CheckSpriteDirectoryExists(_mockMock, "outfit", null);
        
        Assert.That(error, Is.EqualTo(string.Format(Resources.ItemDoesNotHaveValue, "outfit")));
    }
    
    [Test]
    public void ShouldCheckIfSpriteDirectoryIsNotEmpty()
    {
        var error = ValidationTools.CheckSpriteDirectoryExists(_mockMock, "outfit", "");
        
        Assert.That(error, Is.EqualTo(string.Format(Resources.ItemDoesNotHaveValue, "outfit")));
    }
    
    [Test]
    public void ShouldCheckIfSpriteDirectoryExists()
    {
        var error = ValidationTools.CheckSpriteDirectoryExists(_mockMock, "outfit", "images/not-found");
        
        Assert.That(error, Is.EqualTo(string.Format(Resources.SpriteFolderDoesNotExist, "outfit", "images/not-found")));
    }
    
    [Test]
    public void ShouldCheckIfSpriteDirectoryExistsAsDirectory()
    {
        var error = ValidationTools.CheckSpriteDirectoryExists(_mockMock, "outfit", "images/ui.png");
        
        Assert.That(error, Is.EqualTo(string.Format(Resources.SpriteFolderDoesNotExist, "outfit", "images/ui.png")));
    }
    
    [Test]
    public void ShouldCheckIfSpriteDirectoryIsNotEmptyDirectory()
    {
        var error = ValidationTools.CheckSpriteDirectoryExists(_mockMock, "outfit", "images/empty");
        
        Assert.That(error, Is.EqualTo(string.Format(Resources.SpriteFolderIsEmpty, "outfit", "images/empty")));
    }
}