using Garethp.ModsOfMistriaInstallerLib.Generator;
using Garethp.ModsOfMistriaInstallerLib.Installer;
using Garethp.ModsOfMistriaInstallerLib.Lang;
using ModsOfMistriaInstallerLibTests.Fixtures;
using ModsOfMistriaInstallerLibTests.TestUtils;
using Newtonsoft.Json.Linq;

namespace ModsOfMistriaInstallerLibTests.EndToEnd;

[TestFixture]
public class NewItemsTest
{
    private MockFileModifier _fileModifier;

    private readonly MockInstaller _installer = new([
        new NewItemsGenerator(),
    ], [
        new FiddleInstaller()
    ]);

    [SetUp]
    public void SetUp()
    {
        _fileModifier = new(new Dictionary<string, string>
        {
            { "__fiddle__.json", "{ }" }
        });
    }

    [Test]
    public void ShouldInstallValidItemToFiddle()
    {
        var mod = new MockMod((new Dictionary<string, string>
        {
            {
                "items/test.json", new JObject
                {
                    {
                        "new_item", new JObject
                        {
                            { "overwrites_other_mod", false },
                            { "foo", "bar" }
                        }
                    },
                }.ToString()
            }
        }));

        var expected = new JObject
        {
            { "extras/objects", new JArray() },
            { "extras", new JObject {
                { "objects", new JArray() },
                { "items", new JArray
                {
                    new JObject
                    {
                        { "name", "new_item" },
                        { "data", new JObject
                        {
                            { "foo", "bar" }
                        }}
                    }
                } }
            } },
            { "extras/items", new JArray
            {
                new JObject
                {
                    { "name", "new_item" },
                    { "data", new JObject
                    {
                        { "foo", "bar" }
                    }}
                }
            } }
        };

        _installer.ValidateMods([mod]);
        
        Assert.That(mod.GetValidation(), Is.EqualTo(new Validation()).Using(new ValidationComparer()));
        
        _installer.InstallMods([mod], _fileModifier);
        
        Assert.That(_fileModifier.GetFile("__fiddle__.json"), new MatchesJsonConstraint(expected));
    }

    [Test]
    public void ShouldValidateOverwritesOtherModIsRequired()
    {
        var mod = new MockMod((new Dictionary<string, string>
        {
            {
                "items/test.json", new JObject
                {
                    {
                        "new_item", new JObject
                        {
                            { "foo", "bar" }
                        }
                    },
                }.ToString()
            }
        }));

        _installer.ValidateMods([mod]);

        var expected = new Validation();
        expected.AddError(mod, "items/test.json", string.Format(Resources.CoreErrorNewItemHasNoOverwritesOtherMod, "new_item"));
        
        Assert.That(mod.GetValidation(), Is.EqualTo(expected).Using(new ValidationComparer()));
    }
}