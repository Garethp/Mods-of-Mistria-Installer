using System.IO.Compression;
using System.Text;
using Garethp.ModsOfMistriaInstallerLib;

namespace ModsOfMistriaInstallerLibTests;

[TestFixture]
public class LocationDiagnosticsTest
{
    private string _root = "";

    [SetUp]
    public void SetUp() => _root = Path.Combine(Path.GetTempPath(), "momi_location_" + Path.GetRandomFileName());

    [TearDown]
    public void TearDown()
    {
        if (Directory.Exists(_root)) Directory.Delete(_root, true);
    }

    [Test]
    public void ExplainsMissingGameMarker()
    {
        Directory.CreateDirectory(_root);

        Assert.That(LocationDiagnostics.DescribeGame(_root), Does.Contain("Maybe.toml is missing"));
    }

    [Test]
    public void ExplainsMissingGameArchive()
    {
        Directory.CreateDirectory(_root);
        File.WriteAllText(Path.Combine(_root, "Maybe.toml"), "");

        Assert.That(LocationDiagnostics.DescribeGame(_root), Does.Contain("assets.zip"));
    }

    [Test]
    public void ExplainsMissingModsFolder()
    {
        Directory.CreateDirectory(_root);

        Assert.That(LocationDiagnostics.DescribeMods(_root, ""), Does.Contain("No mods folder"));
    }

    [Test]
    public void AcceptsValidGameAndModsLocations()
    {
        Directory.CreateDirectory(_root);
        File.WriteAllText(Path.Combine(_root, "Maybe.toml"), "");
        using (var archive = ZipFile.Open(Path.Combine(_root, "assets.zip"), ZipArchiveMode.Create))
        using (var stream = archive.CreateEntry("assets/Maybe.txt").Open())
            stream.Write(Encoding.UTF8.GetBytes("fixture"));
        var mods = Path.Combine(_root, "mods");
        Directory.CreateDirectory(mods);

        Assert.That(LocationDiagnostics.DescribeGame(_root), Is.EqualTo("Fields of Mistria installation detected."));
        Assert.That(LocationDiagnostics.DescribeMods(_root, mods), Is.EqualTo("Mods folder detected."));
    }

    [Test]
    public void ExplainsDamagedGameArchive()
    {
        Directory.CreateDirectory(_root);
        File.WriteAllText(Path.Combine(_root, "Maybe.toml"), "");
        File.WriteAllText(Path.Combine(_root, "assets.zip"), "not a zip");

        Assert.That(LocationDiagnostics.DescribeGame(_root), Does.Contain("damaged"));
    }
}
