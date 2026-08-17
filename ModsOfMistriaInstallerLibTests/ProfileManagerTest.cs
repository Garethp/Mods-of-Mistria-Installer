using Garethp.ModsOfMistriaInstallerLib;
using ModsOfMistriaInstallerLibTests.Fixtures;

namespace ModsOfMistriaInstallerLibTests;

public class ProfileManagerTest
{
    [Test]
    public void LoadOrderDoesNotCollapsePhysicalCopiesWithTheSameId()
    {
        var first = new MockMod(new List<string>()) { Id = "same.mod", DirName = "first.zip" };
        var second = new MockMod(new List<string>()) { Id = "same.mod", DirName = "second" };
        var other = new MockMod(new List<string>()) { Id = "other.mod", DirName = "other" };

        var sorted = ProfileManager.SortByLoadOrder(
            [first, second, other],
            ["same.mod", "other.mod"]);

        Assert.That(sorted, Has.Count.EqualTo(3));
        Assert.That(sorted[0], Is.SameAs(first));
        Assert.That(sorted[1], Is.SameAs(second));
        Assert.That(sorted[2], Is.SameAs(other));
    }

    [Test]
    public void SavedLoadOrderSurvivesManagerReloadWithoutReplacingModIds()
    {
        var directory = Path.Combine(Path.GetTempPath(), "momi-profile-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);

        try
        {
            var manager = new ProfileManager(directory);
            manager.CreateProfile("Secondary Profile");
            manager.SwitchProfile("Secondary Profile");
            manager.SaveCurrentProfile(
                ["deulo.wiki", "atd.atds_farmer"],
                ["deulo.wiki", "atd.atds_farmer"]);

            var reloaded = new ProfileManager(directory);
            var (enabled, order) = reloaded.GetCurrentProfile();

            Assert.That(reloaded.CurrentProfileName, Is.EqualTo("Secondary Profile"));
            Assert.That(enabled, Is.EqualTo(new[] { "deulo.wiki", "atd.atds_farmer" }));
            Assert.That(order, Is.EqualTo(new[] { "deulo.wiki", "atd.atds_farmer" }));
        }
        finally
        {
            if (Directory.Exists(directory)) Directory.Delete(directory, recursive: true);
        }
    }
}
