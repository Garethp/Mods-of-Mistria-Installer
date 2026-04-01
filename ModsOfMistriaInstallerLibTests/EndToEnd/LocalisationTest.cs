using Garethp.ModsOfMistriaInstallerLib.Generator;
using Garethp.ModsOfMistriaInstallerLib.Installer;
using ModsOfMistriaInstallerLibTests.Fixtures;
using ModsOfMistriaInstallerLibTests.Utils;
using Newtonsoft.Json.Linq;

namespace ModsOfMistriaInstallerLibTests.EndToEnd;

public class LocalisationTest
{
    private MockFileModifier _fileModifier;

    private readonly MockInstaller _installer = new([
        new LocalisationGenerator()
    ], [
        new LocalisationInstaller()
    ]);

    [SetUp]
    public void SetUp()
    {
        _fileModifier = new(new Dictionary<string, string>
        {
            { "localization.json", "{}" }
        });
    }

    [Test]
    public void ShouldDefaultToEnglish()
    {
        var mod = new MockMod(new Dictionary<string, string>
        {
            {
                "localisation/test.json", new JObject
                {
                    { "test_key", "test_value" }
                }.ToString()
            }
        });
        
        _installer.InstallMods([mod], _fileModifier);
        
        Assert.That(_fileModifier.GetFile("localization.json"), new MatchesJsonConstraint(new JObject
        {
            { "eng", new JObject
            {
                { "test_key", "test_value" }
            }}
        }));
    }
    
    [Test]
    public void ShouldPullLanguageFromFileName()
    {
        var mod = new MockMod(new Dictionary<string, string>
        {
            {
                "localisation/test.jpn.json", new JObject
                {
                    { "test_key", "試験値" }
                }.ToString()
            }
        });
        
        _installer.InstallMods([mod], _fileModifier);
        
        Assert.That(_fileModifier.GetFile("localization.json"), new MatchesJsonConstraint(new JObject
        {
            { "jpn", new JObject
            {
                { "test_key", "試験値" }
            }}
        }));
    }
    
    [Test]
    public void ShouldEnsureKeysInAllLanguages()
    {
        _fileModifier = new(new Dictionary<string, string>
        {
            { "localization.json", new JObject
            {
                { "jpn", new JObject() }
            }.ToString()}
        });
        
        var mod = new MockMod(new Dictionary<string, string>
        {
            {
                "localisation/test.json", new JObject
                {
                    { "test_key", "test_value" }
                }.ToString()
            }
        });
        
        _installer.InstallMods([mod], _fileModifier);
        
        Assert.That(_fileModifier.GetFile("localization.json"), new MatchesJsonConstraint(new JObject
        {
            { "jpn", new JObject
            {
                { "test_key", "MISSING" }
            }},
            { "eng", new JObject
            {
                { "test_key", "test_value" }
            }}
        }));
    }

    [Test]
    public void ShouldReadFromLocalisationAndLocalization()
    {
        var mod = new MockMod(new Dictionary<string, string>
        {
            {
                "localisation/test.json", new JObject
                {
                    { "foo", "bar" }
                }.ToString()
            },
            {
                "localization/test.json", new JObject
                {
                    { "bar", "baz" }
                }.ToString()
            }
        });
        
        _installer.InstallMods([mod], _fileModifier);
        
        Assert.That(_fileModifier.GetFile("localization.json"), new MatchesJsonConstraint(new JObject
        {
            { "eng", new JObject
            {
                { "foo", "bar" },
                { "bar", "baz" }
            }}
        }));
    }
}