using System.IO.Compression;
using System.Text;
using Garethp.ModsOfMistriaInstallerLib.Seam;

namespace ModsOfMistriaInstallerLibTests.Seam;

[TestFixture]
public class ZipPristineSourceTest
{
    private string _root = "";
    private string _zipPath = "";

    [SetUp]
    public void CreateTempDir()
    {
        _root = Path.Combine(Path.GetTempPath(), "momi_pristine_" + Path.GetRandomFileName());
        Directory.CreateDirectory(_root);
        _zipPath = Path.Combine(_root, "assets.zip");
    }

    [TearDown]
    public void RemoveTempDir()
    {
        Directory.Delete(_root, true);
    }

    private void WriteZip(params string[] names)
    {
        if (File.Exists(_zipPath)) File.Delete(_zipPath);
        using var archive = ZipFile.Open(_zipPath, ZipArchiveMode.Create);
        foreach (var name in names)
        {
            using var stream = archive.CreateEntry(name).Open();
            stream.Write(Encoding.UTF8.GetBytes("// x\n"));
        }
    }

    [Test]
    public void ShouldAnswerMembershipFromTheCachedSet()
    {
        WriteZip("assets/a.gml", "assets/b.gml");

        using var pristine = new ZipPristineSource(_zipPath);

        Assert.That(pristine.Has("assets/a.gml"), Is.True);
        Assert.That(pristine.Has("assets/b.gml"), Is.True);
        Assert.That(pristine.Has("assets/ghost.gml"), Is.False);
        Assert.That(pristine.Read("assets/a.gml"), Is.EqualTo(Encoding.UTF8.GetBytes("// x\n")));
        Assert.That(pristine.Read("assets/ghost.gml"), Is.Null);
    }

    [Test]
    public void ShouldAnswerForTheNewArchiveAfterReopen()
    {
        // a game update swaps the zip: a fresh accessor must answer for the
        // NEW archive, never the old name set
        WriteZip("assets/a.gml");
        var first = new ZipPristineSource(_zipPath);
        Assert.That(first.Has("assets/a.gml"), Is.True);
        first.Dispose();

        WriteZip("assets/b.gml");

        using var second = new ZipPristineSource(_zipPath);
        Assert.That(second.Has("assets/a.gml"), Is.False);
        Assert.That(second.Has("assets/b.gml"), Is.True);
    }

    [Test]
    public void ShouldThrowWhenTheArchiveIsMissing()
    {
        Assert.Throws<FileNotFoundException>(() => new ZipPristineSource(_zipPath));
    }

    [Test]
    public void ShouldListGmlFilesSorted()
    {
        WriteZip("assets/gml/objects/B.gml", "assets/gml/objects/A.gml", "assets/sprites/x.png",
            "assets/gml/notes.txt");

        using var pristine = new ZipPristineSource(_zipPath);

        Assert.That(pristine.GmlFiles(), Is.EqualTo(new[]
        {
            "assets/gml/objects/A.gml",
            "assets/gml/objects/B.gml",
        }));
    }
}
