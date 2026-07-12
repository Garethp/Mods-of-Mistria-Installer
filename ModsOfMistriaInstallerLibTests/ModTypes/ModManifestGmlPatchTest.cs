using Garethp.ModsOfMistriaInstallerLib.ModTypes;
using Newtonsoft.Json.Linq;
using Tomlyn;
using Tomlyn.Model;

namespace ModsOfMistriaInstallerLibTests.ModTypes;

[TestFixture]
public class ModManifestGmlPatchTest
{
    [Test]
    public void FromJsonParsesGmlPatchesAndDefaultsExpectedMatches()
    {
        var manifest = ModManifest.FromJson(JObject.Parse("""
            {
              "name": "Patch Mod",
              "author": "Tester",
              "version": "1.0.0",
              "gmlPatches": [
                {
                  "id": "example.learn-food",
                  "target": "gml/scripts/Items/UseItem.gml",
                  "operation": "insert_after",
                  "anchorFile": "patches/learn-food.anchor.gml",
                  "contentFile": "patches/learn-food.content.gml",
                  "expectedMatches": 2
                },
                {
                  "id": "example.learn-drink",
                  "target": "gml/scripts/Items/UseItem.gml",
                  "operation": "insert_before",
                  "anchorFile": "patches/learn-drink.anchor.gml",
                  "contentFile": "patches/learn-drink.content.gml"
                }
              ]
            }
            """));

        Assert.That(manifest.GmlPatches, Is.EqualTo(new List<GmlPatchDefinition>
        {
            new(
                "example.learn-food",
                "gml/scripts/Items/UseItem.gml",
                "insert_after",
                "patches/learn-food.anchor.gml",
                "patches/learn-food.content.gml",
                2),
            new(
                "example.learn-drink",
                "gml/scripts/Items/UseItem.gml",
                "insert_before",
                "patches/learn-drink.anchor.gml",
                "patches/learn-drink.content.gml",
                1)
        }));
    }

    [Test]
    public void FromTomlParsesGmlPatchTableArray()
    {
        var toml = TomlSerializer.Deserialize<TomlTable>("""
            name = "Patch Mod"
            author = "Tester"
            version = "1.0.0"

            [[gmlPatches]]
            id = "example.learn-food"
            target = "gml/scripts/Items/UseItem.gml"
            operation = "replace_exact"
            anchorFile = "patches/learn-food.anchor.gml"
            contentFile = "patches/learn-food.content.gml"
            expectedMatches = 3
            """)!;

        var manifest = ModManifest.FromToml(toml);

        Assert.That(manifest.GmlPatches, Is.EqualTo(new List<GmlPatchDefinition>
        {
            new(
                "example.learn-food",
                "gml/scripts/Items/UseItem.gml",
                "replace_exact",
                "patches/learn-food.anchor.gml",
                "patches/learn-food.content.gml",
                3)
        }));
    }

    [Test]
    public void MissingGmlPatchesProducesEmptyList()
    {
        var manifest = ModManifest.FromJson(JObject.Parse("""
            {
              "name": "No Patch Mod",
              "author": "Tester",
              "version": "1.0.0"
            }
            """));

        Assert.That(manifest.GmlPatches, Is.Empty);
    }
}
