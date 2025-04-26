using Garethp.ModsOfMistriaInstallerLib.Models;
using Garethp.ModsOfMistriaInstallerLib.ModTypes;
using ModsOfMistriaInstallerLibTests.Fixtures;
using Newtonsoft.Json;

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
        var newItem = new NewItem
        {
            Name = "new_item",
            Prefix = "mod_id",
            Data = new {
                dummy = "data"
            }
        };

        return newItem;
    }
    
    [Test]
    public void ShouldAddThePrefixToNameByDefault()
    {
        var newItem = GetNewItem();
        
        Assert.That(newItem.Name, Is.EqualTo("mod_id_new_item"));
    }
    
    [Test]
    public void ShouldAllowDisablingThePrefix()
    {
        var newItem = GetNewItem();
        newItem.DisablePrefix = true;
        
        Assert.That(newItem.Name, Is.EqualTo("new_item"));
    }
    
    [TestCase]
    public void ShouldNotSerializeUnneededProperties()
    {
        var newItem = GetNewItem();
        newItem.DisablePrefix = true;
        var json = JsonConvert.SerializeObject(newItem);
        
        Assert.That(json, Does.Not.Contain("prefix"));
        Assert.That(json, Does.Not.Contain("disable_prefix"));
    }
}