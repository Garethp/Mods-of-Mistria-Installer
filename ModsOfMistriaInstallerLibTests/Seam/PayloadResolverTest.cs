using System.Text;
using Garethp.ModsOfMistriaInstallerLib.Seam;

namespace ModsOfMistriaInstallerLibTests.Seam;

[TestFixture]
public class PayloadResolverTest
{
    [Test]
    public void ShouldReturnTheEmbeddedCatalog()
    {
        var (name, bytes) = PayloadResolver.SeamCatalog();

        Assert.That(name, Is.EqualTo("Seam/Payload/seams.toml"));
        var catalog = SeamCatalogLoader.Load(bytes, name);
        Assert.That(catalog.Version, Is.EqualTo(2));
        Assert.That(catalog.Entries, Is.Not.Empty);
    }

    [Test]
    public void ShouldPreferTheCatalogOverride()
    {
        var path = Path.Combine(Path.GetTempPath(), "momi_catalog_" + Path.GetRandomFileName() + ".toml");
        File.WriteAllText(path, "version = 2\n");
        Environment.SetEnvironmentVariable("MOMI_SEAM_CATALOG", path);

        try
        {
            var (name, bytes) = PayloadResolver.SeamCatalog();

            Assert.That(name, Is.EqualTo(path));
            Assert.That(Encoding.UTF8.GetString(bytes), Is.EqualTo("version = 2\n"));
        }
        finally
        {
            Environment.SetEnvironmentVariable("MOMI_SEAM_CATALOG", null);
            File.Delete(path);
        }
    }

    [Test]
    public void ShouldFailLoudlyOnAMissingOverride()
    {
        var path = Path.Combine(Path.GetTempPath(), "momi_catalog_" + Path.GetRandomFileName() + ".toml");
        Environment.SetEnvironmentVariable("MOMI_SEAM_CATALOG", path);

        try
        {
            Assert.Throws<FileNotFoundException>(() => PayloadResolver.SeamCatalog());
        }
        finally
        {
            Environment.SetEnvironmentVariable("MOMI_SEAM_CATALOG", null);
        }
    }
}
