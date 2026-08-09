using Garethp.ModsOfMistriaInstallerLib;
using System.Runtime.InteropServices;

namespace ModsOfMistriaInstallerLibTests;

[TestFixture]
public class MistriaLocatorTest
{
    [Test]
    public void FindsAllSupportedModsFolderNameVariantsNextToTheGame()
    {
        var root = Path.Combine(Path.GetTempPath(), "momi_locator_" + Path.GetRandomFileName());
        Directory.CreateDirectory(root);
        File.WriteAllText(Path.Combine(root, "Maybe.toml"), "");

        try
        {
            foreach (var name in new[] { "mods", "Mods", "MODS", "MODs" })
            {
                var folder = Path.Combine(root, name);
                Directory.CreateDirectory(folder);
                var found = MistriaLocator.GetModsLocation(root);
                Assert.That(found, Is.Not.Null);
                // Standard Windows and macOS volumes are commonly
                // case-insensitive. On those filesystems, mods/Mods/MODS
                // are the same directory even though Linux can distinguish
                // them.
                var comparison = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ||
                                  RuntimeInformation.IsOSPlatform(OSPlatform.OSX)
                    ? StringComparison.OrdinalIgnoreCase
                    : StringComparison.Ordinal;
                Assert.That(string.Equals(found, Path.GetFullPath(folder), comparison), Is.True);
                Directory.Delete(folder);
            }
        }
        finally
        {
            Directory.Delete(root, true);
        }
    }
}
