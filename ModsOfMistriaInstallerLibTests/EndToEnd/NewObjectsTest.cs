using Garethp.ModsOfMistriaInstallerLib.Generator;
using Garethp.ModsOfMistriaInstallerLib.Installer;
using Garethp.ModsOfMistriaInstallerLib.Lang;
using ModsOfMistriaInstallerLibTests.Fixtures;
using ModsOfMistriaInstallerLibTests.TestUtils;
using Newtonsoft.Json.Linq;

namespace ModsOfMistriaInstallerLibTests.EndToEnd;

public class NewObjectsTest
{
     private MockFileModifier _fileModifier;

    private readonly MockInstaller _installer = new([
        new NewObjectsGenerator(),
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
        var mod = new MockMod(new Dictionary<string, string>
        {
            {
                "objects/test.json", new JObject
                {
                    {
                        "new_object", new JObject
                        {
                            { "category", "building" },
                            { "overwrites_other_mod", false },
                            { "data", new JObject
                            {
                                { "foo", "bar" }
                            } }
                        }
                    },
                }.ToString()
            }
        });

        var expected = new JObject
        {
            { "extras/items", new JArray() },
            { "extras", new JObject {
                { "items", new JArray() },
                { "objects", new JArray
                {
                    new JObject
                    {
                        { "name", "new_object" },
                        { "category", "building" },
                        { "data", new JObject
                        {
                            { "foo", "bar" }
                        }}
                    }
                } }
            } },
            { "extras/objects", new JArray
            {
                new JObject
                {
                    { "name", "new_object" },
                    { "category", "building" },
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
        var mod = new MockMod(new Dictionary<string, string>
        {
            {
                "objects/test.json", new JObject
                {
                    {
                        "new_object", new JObject
                        {
                            { "category", "building" },
                            { "data", new JObject
                            {
                                { "foo", "bar" }
                            } }
                        }
                    },
                }.ToString()
            }
        });

        _installer.ValidateMods([mod]);

        var expected = new Validation();
        expected.AddError(mod, "objects/test.json", string.Format(Resources.CoreErrorNewObjectHasNoOverwritesOtherMod, "new_object"));
        
        Assert.That(mod.GetValidation(), Is.EqualTo(expected).Using(new ValidationComparer()));
    }
}