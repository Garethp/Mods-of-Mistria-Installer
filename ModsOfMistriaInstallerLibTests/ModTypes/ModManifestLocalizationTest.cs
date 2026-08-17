using Garethp.ModsOfMistriaInstallerLib.ModTypes;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using Tomlyn;
using Tomlyn.Model;

namespace ModsOfMistriaInstallerLibTests.ModTypes;

public class ModManifestLocalizationTest
{
    [Test]
    public void JsonReadsLocalizedNameAndDescriptionWithFallback()
    {
        var manifest = ModManifest.FromJson(JObject.Parse("""
            {
              "name": "Example Mod",
              "name_bg": "Примерен мод",
              "description": "English description",
              "description_bg": "Българско описание"
            }
            """));

        Assert.That(ModManifest.LocalizedValue(manifest.LocalizedNames, manifest.Name, "bg"),
            Is.EqualTo("Примерен мод"));
        Assert.That(ModManifest.LocalizedValue(manifest.LocalizedDescriptions, "", "bg"),
            Is.EqualTo("Българско описание"));
        Assert.That(ModManifest.LocalizedValue(manifest.LocalizedNames, manifest.Name, "pl"),
            Is.EqualTo("Example Mod"));
    }

    [Test]
    public void TomlAcceptsRegionalLanguageKeyAndFallsBackToBaseLanguage()
    {
        var manifest = ModManifest.FromToml(TomlSerializer.Deserialize<TomlTable>("""
            name = "Example Mod"
            name_pt_br = "Mod de exemplo"
            description = "English description"
            description_bg = "Българско описание"
            """)!);

        Assert.That(ModManifest.LocalizedValue(manifest.LocalizedNames, manifest.Name, "pt-br"),
            Is.EqualTo("Mod de exemplo"));
        Assert.That(ModManifest.LocalizedValue(manifest.LocalizedDescriptions, "", "bg-BG"),
            Is.EqualTo("Българско описание"));
    }
}
