using Garethp.ModsOfMistriaInstallerLib.Generator;
using Garethp.ModsOfMistriaInstallerLib.Installer;
using ModsOfMistriaInstallerLibTests.Fixtures;
using ModsOfMistriaInstallerLibTests.Utils;
using Newtonsoft.Json.Linq;

namespace ModsOfMistriaInstallerLibTests.EndToEnd;

public class MacroLinesTest
{
    private MockFileModifier _fileModifier;

    private readonly MockInstaller _installer = new([
        new LocalisationGenerator()
    ], [
        new MacroLinesInstaller(),
        new LocalisationInstaller()
    ]);

    [SetUp]
    public void SetUp()
    {
        _fileModifier = new(new Dictionary<string, string>
        {
            { "macro_lines.json", "{}" },
            { "localization.json", "{}" }
        });
    }

    [TestCase("@she")]
    [TestCase("@he")]
    [TestCase("@they")]
    [TestCase("@it")]
    [TestCase("@none")]
    public void ShouldAddMacroLines(string macro)
    {
        var mod = new MockMod(new Dictionary<string, string>
        {
            {
                "localisation/test.json", new JObject
                {
                    { $"test_key{macro}", "test_value" }
                }.ToString()
            }
        });
        
        _installer.InstallMods([mod], _fileModifier);
        
        Assert.That(_fileModifier.GetFile("macro_lines.json"), new MatchesJsonConstraint(new JObject
        {
            { "eng", new JArray { "test_key" } }
        }));

        Assert.That(_fileModifier.GetFile("localization.json"), new MatchesJsonConstraint(new JObject
        {
            { "eng", new JObject
            {
                { $"test_key{macro}", "test_value" }
            } }
        }));
    }

    [Test]
    public void ShouldNotDuplicateExistingKeys()
    {
        _fileModifier = new(new Dictionary<string, string>
        {
            { "macro_lines.json", new JObject
            {
                { "eng", new JArray { "test_key" }}
            }.ToString()},
            { "localization.json", "{}" }
        });
        
        var mod = new MockMod(new Dictionary<string, string>
        {
            {
                "localisation/test.json", new JObject
                {
                    { "test_key@she", "test_value" }
                }.ToString()
            }
        });
        
        _installer.InstallMods([mod], _fileModifier);
        
        Assert.That(_fileModifier.GetFile("macro_lines.json"), new MatchesJsonConstraint(new JObject
        {
            { "eng", new JArray { "test_key" } }
        }));
    }

    [Test]
    public void ShouldNotDuplicateNewKeys()
    {
        _fileModifier = new(new Dictionary<string, string>
        {
            { "macro_lines.json", "{}" },
            { "localization.json", "{}" }
        });
        
        var mod = new MockMod(new Dictionary<string, string>
        {
            {
                "localisation/test.json", new JObject
                {
                    { "test_key@she", "test_value" }
                }.ToString()
            },
            {
                "localization/test.json", new JObject
                {
                    { "test_key@she", "test_value" }
                }.ToString()
            }
        });
        
        _installer.InstallMods([mod], _fileModifier);
        
        Assert.That(_fileModifier.GetFile("macro_lines.json"), new MatchesJsonConstraint(new JObject
        {
            { "eng", new JArray { "test_key" } }
        }));
    }
}