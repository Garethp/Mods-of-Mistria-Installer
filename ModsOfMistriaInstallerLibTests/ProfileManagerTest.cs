using Garethp.ModsOfMistriaInstallerLib;

namespace ModsOfMistriaInstallerLibTests;

public class ProfileManagerTest
{
    [Test]
    public void SavedLoadOrderSurvivesManagerReloadWithoutReplacingModIds()
    {
        var directory = Path.Combine(Path.GetTempPath(), "momi-profile-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);

        try
        {
            var manager = new ProfileManager(directory);
            manager.CreateProfile("Bulgarian");
            manager.SwitchProfile("Bulgarian");
            manager.SaveCurrentProfile(
                ["deulo.wiki", "atd.atds_farmer"],
                ["deulo.wiki", "atd.atds_farmer"]);

            var reloaded = new ProfileManager(directory);
            var (enabled, order) = reloaded.GetCurrentProfile();

            Assert.That(reloaded.CurrentProfileName, Is.EqualTo("Bulgarian"));
            Assert.That(enabled, Is.EqualTo(new[] { "deulo.wiki", "atd.atds_farmer" }));
            Assert.That(order, Is.EqualTo(new[] { "deulo.wiki", "atd.atds_farmer" }));
        }
        finally
        {
            if (Directory.Exists(directory)) Directory.Delete(directory, recursive: true);
        }
    }
}
