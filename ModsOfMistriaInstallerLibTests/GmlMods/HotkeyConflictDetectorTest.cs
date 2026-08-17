using Garethp.ModsOfMistriaInstallerLib.GmlMods;
using Garethp.ModsOfMistriaInstallerLib.ModTypes;
using ModsOfMistriaInstallerLibTests.Fixtures;

namespace ModsOfMistriaInstallerLibTests.GmlMods;

[TestFixture]
public class HotkeyConflictDetectorTest
{
    [Test]
    public void FindsMacroAndDirectVirtualKeyConflicts()
    {
        var wiki = new MockMod(new Dictionary<string, object>
        {
            ["gml/wiki.gml"] = "#macro WIKI_DEFAULT_KEY \"F6\""
        }) { Id = "wiki" };
        var other = new MockMod(new Dictionary<string, object>
        {
            ["gml/other.gml"] = "if (keyboard_check_pressed(vk_f6)) { }"
        }) { Id = "other" };

        var conflicts = HotkeyConflictDetector.Find([wiki, other]);

        Assert.That(conflicts.Select(conflict => conflict.Key), Does.Contain("F6"));
        Assert.That(conflicts.Single(conflict => conflict.Key == "F6").Usages,
            Has.Count.EqualTo(2));
    }

    [Test]
    public void FindsAuxiliaryBagDefaultRangeAgainstF8ModOnlyWhenShared()
    {
        var aux = new MockMod(new Dictionary<string, object>
        {
            ["gml/aux.gml"] = "function mah_default_hotkey_name(index) { return \"f\"; }\nfunction mah_hotkey_slot_7() {}"
        }) { Id = "aux" };
        var f8 = new MockMod(new Dictionary<string, object>
        {
            ["gml/f8.gml"] = "#macro KEY \"F8\""
        }) { Id = "f8" };

        var conflicts = HotkeyConflictDetector.Find([aux, f8]);

        Assert.That(conflicts, Is.Empty);
    }

    [Test]
    public void IgnoresDocumentationFilesInFileConflicts()
    {
        var alpha = new MockMod(new Dictionary<string, object>
        {
            ["README.md"] = "a",
            ["README.txt"] = "a"
        }) { Id = "alpha" };
        var beta = new MockMod(new Dictionary<string, object>
        {
            ["README.md"] = "b",
            ["README.txt"] = "b"
        }) { Id = "beta" };

        Assert.That(ModFileConflictDetector.Find([alpha, beta]), Is.Empty);
    }
}
