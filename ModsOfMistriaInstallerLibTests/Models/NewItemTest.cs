using Garethp.ModsOfMistriaInstallerLib.Generator;
using Garethp.ModsOfMistriaInstallerLib.Lang;
using Garethp.ModsOfMistriaInstallerLib.Models;
using Garethp.ModsOfMistriaInstallerLib.ModTypes;
using ModsOfMistriaInstallerLibTests.Fixtures;

namespace ModsOfMistriaInstallerLibTests.Models;

[TestFixture]
public class NewItemTest
{
    private IMod _mockMod;

    [SetUp]
    public void SetUp()
    {
        _mockMod = new MockMod([
        ]);
    }

    private static NewItem GetNewItem()
    {
        var newItem = new NewItem()
        {
            Name = "new_object",
            OverwritesOtherMod = false,
            Data = new {
                dummy = "data"
            }
        };

        return newItem;
    }

    [Test]
    public void ShouldNotSerializeOverwritesOtherMod()
    {
        var newItem = GetNewItem();
        Assert.That(newItem.ShouldSerializeOverwritesOtherMod(), Is.False);
    }

    [Test]
    public void ShouldValidateOverwritesOtherModIsPresent()
    {
        var newItem = GetNewItem();
        newItem.OverwritesOtherMod = null;
        
        var validation = newItem.Validate(new Validation(), _mockMod, "new_item.json", "id");
        var expectedValidation = new Validation();
        expectedValidation.AddError(_mockMod, "new_item.json", string.Format(Resources.ErrorNewItemHasNoOverwritesOtherMod, newItem.Name));
        
        Assert.That(validation, Is.EqualTo(expectedValidation).Using(new ValidationComparer()));
    }
}