using System.IO.Compression;
using Garethp.ModsOfMistriaInstallerLib.GmlMods;
using Garethp.ModsOfMistriaInstallerLib.ModTypes;

namespace Garethp.ModsOfMistriaInstallerLibTests.ModTypes;

[TestFixture]
public class ZipModPathNormalizationTest
{
    [Test]
    public void ReadsGmlWhenArchiveUsesBackslashes()
    {
        using var buffer = new MemoryStream();
        using (var archive = new ZipArchive(buffer, ZipArchiveMode.Create, leaveOpen: true))
        {
            Write(archive, "AutoReel\\manifest.json", "{\"name\":\"AutoReel\",\"author\":\"Test\",\"version\":\"1.0.0\",\"minInstallerVersion\":\"0.15.1\"}");
            Write(archive, "AutoReel\\gml\\AutoReel.gml", "// test gml");
        }

        buffer.Position = 0;
        using var archiveToRead = new ZipArchive(buffer, ZipArchiveMode.Read, leaveOpen: false);
        var mod = new ZipMod(archiveToRead, "AutoReel\\");

        Assert.That(mod.FileExists("gml/AutoReel.gml"), Is.True);
        Assert.That(mod.GetAllFiles(".gml"), Has.Some.EqualTo("AutoReel/gml/AutoReel.gml"));

        var code = GmlModCollector.Collect(mod);
        Assert.That(code, Is.Not.Null);
        Assert.That(code!.GmlFiles, Has.Some.EqualTo("gml/AutoReel.gml"));
        Assert.That(code.Read("gml/AutoReel.gml"), Is.EqualTo(System.Text.Encoding.UTF8.GetBytes("// test gml")));
    }

    private static void Write(ZipArchive archive, string path, string content)
    {
        using var writer = new StreamWriter(archive.CreateEntry(path).Open());
        writer.Write(content);
    }
}
