using Garethp.ModsOfMistriaInstallerLib.Utils;
using Tomlyn.Model;

namespace ModsOfMistriaInstallerLibTests.Utils;

// ParseDocument builds the document from the event parser: TomlSerializer
// replaces a table array when its [[name]] group re-opens, where the TOML
// spec appends. Every case here is a shape the shipped seam catalog uses.
[TestFixture]
public class TomlTest
{
    [Test]
    public void ShouldAppendAReopenedTableArrayGroup()
    {
        var doc = Toml.ParseDocument("""
            [[hook]]
            name = "first"

            [[seam]]
            id = "between"

            [[hook]]
            name = "second"
            """);

        var hooks = (TomlTableArray)doc["hook"];
        Assert.That(hooks, Has.Count.EqualTo(2));
        Assert.That(hooks[0]["name"], Is.EqualTo("first"));
        Assert.That(hooks[1]["name"], Is.EqualTo("second"));
        Assert.That((TomlTableArray)doc["seam"], Has.Count.EqualTo(1));
    }

    [Test]
    public void ShouldParseScalarValues()
    {
        var doc = Toml.ParseDocument("""
            version = 2
            name = "hook"
            in_place = true
            weight = 1.5
            """);

        Assert.That(doc["version"], Is.EqualTo(2L));
        Assert.That(doc["name"], Is.EqualTo("hook"));
        Assert.That(doc["in_place"], Is.True);
        Assert.That(doc["weight"], Is.EqualTo(1.5d));
    }

    [Test]
    public void ShouldReadAMultilineLiteralString()
    {
        var doc = Toml.ParseDocument("""
            [[seam]]
            anchor = '''
            function step_begin() {
            }'''
            """);

        var seam = ((TomlTableArray)doc["seam"])[0];
        Assert.That(seam["anchor"], Is.EqualTo("function step_begin() {\n}"));
    }

    [Test]
    public void ShouldKeepAValueArrayAsAValueArray()
    {
        var doc = Toml.ParseDocument("""
            provides = ["game.step_begin", "game.day_started"]
            empty = []
            """);

        Assert.That(doc["provides"], Is.InstanceOf<TomlArray>());
        Assert.That((TomlArray)doc["provides"], Is.EqualTo(new[] { "game.step_begin", "game.day_started" }));
        Assert.That(doc["empty"], Is.InstanceOf<TomlArray>());
        Assert.That((TomlArray)doc["empty"], Is.Empty);
    }

    [Test]
    public void ShouldNestValueArrays()
    {
        var doc = Toml.ParseDocument("""ctx_fields = [["npc", "self"], ["item", "item"]]""");

        var fields = (TomlArray)doc["ctx_fields"];
        Assert.That(fields, Has.Count.EqualTo(2));
        Assert.That((TomlArray)fields[0]!, Is.EqualTo(new[] { "npc", "self" }));
        Assert.That((TomlArray)fields[1]!, Is.EqualTo(new[] { "item", "item" }));
    }

    [Test]
    public void ShouldAttachAnInlineTableUnderItsKey()
    {
        var doc = Toml.ParseDocument("""
            [[seam]]
            target = { fn = "play", at = "head" }
            """);

        var target = (TomlTable)((TomlTableArray)doc["seam"])[0]["target"];
        Assert.That(target["fn"], Is.EqualTo("play"));
        Assert.That(target["at"], Is.EqualTo("head"));
    }

    // Fatal syntax errors surface as Tomlyn's own exception from the event
    // stream. ParseDocument's FormatException backstop covers only the
    // diagnostics the parser survives.
    [Test]
    public void ShouldThrowOnMalformedToml()
    {
        var exception = Assert.Throws<Tomlyn.TomlException>(() => Toml.ParseDocument("version = \n"));

        Assert.That(exception!.Message, Does.Contain("Missing value"));
    }

    [Test]
    public void ShouldReturnAnEmptyTableForAnEmptyDocument()
    {
        Assert.That(Toml.ParseDocument(""), Is.Empty);
    }
}
