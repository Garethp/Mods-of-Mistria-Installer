using System.IO.Compression;
using System.Text;
using Garethp.ModsOfMistriaInstallerLib.Utils;

namespace ModsOfMistriaInstallerLibTests.Utils;

[TestFixture]
public class ZipFileModifierTest
{
    [Test]
    public void WriteTruncatesExistingEntryAndPreservesUtf8()
    {
        using var archiveStream = new MemoryStream();

        using (var archive = new ZipArchive(archiveStream, ZipArchiveMode.Update, leaveOpen: true))
        {
            var entry = archive.CreateEntry("gml/test.gml");
            using (var writer = new StreamWriter(entry.Open(), new UTF8Encoding(false)))
                writer.Write("This old text is much longer than the replacement.");

            var modifier = new ZipFileModifier(archive);
            modifier.Write("gml/test.gml", "café 🥕");
        }

        archiveStream.Position = 0;
        using var resultArchive = new ZipArchive(archiveStream, ZipArchiveMode.Read);
        using var reader = new StreamReader(
            resultArchive.GetEntry("gml/test.gml")!.Open(),
            Encoding.UTF8,
            detectEncodingFromByteOrderMarks: true);

        Assert.That(reader.ReadToEnd(), Is.EqualTo("café 🥕"));
    }
}
