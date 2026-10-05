using System.IO.Compression;
using System.Text;
using Garethp.ModsOfMistriaInstallerLib.Seam;
using Garethp.ModsOfMistriaInstallerLib.Store;

namespace ModsOfMistriaInstallerLibTests.Store;

// The archive half of the reseed union reads real zips, so these tests build
// real zips. An assets archive whose enum file carries the expander's
// generated-line markers.
[TestFixture]
public class ArchiveMarkerHarvesterTest
{
    private const string Catalog = """
        version = 2

        [[extension]]
        id   = "npc_roster"
        file = "gml/NpcId.gml"

        [extension.ordinal]
        enum     = "NpcId"
        sentinel = "LEN"

        [[extension.sites]]
        id       = "enum_member"
        kind     = "enum_member"
        template = "{{symbol}} = {{ordinal}},"
        indent   = 4

        [extension.vacancy]
        enum_member = "{{symbol}} = {{ordinal}},"
        """ + "\n";

    private static SeamCatalog LoadCatalog() =>
        SeamCatalogLoader.Load(Encoding.UTF8.GetBytes(Catalog), "seams.toml");

    private static string WriteZip(params (string Entry, string Text)[] entries)
    {
        var path = Path.Combine(
            Directory.CreateTempSubdirectory("momi-archive-harvest-test").FullName, "assets.zip");
        using var zip = ZipFile.Open(path, ZipArchiveMode.Create);
        foreach (var (entry, text) in entries)
        {
            using var stream = zip.CreateEntry(entry).Open();
            stream.Write(Encoding.UTF8.GetBytes(text));
        }

        return path;
    }

    [Test]
    public void ShouldHarvestLiveAndVacantMarkerSymbols()
    {
        var archive = WriteZip(("assets/gml/NpcId.gml", """
            enum NpcId {
                Ari,
                Eiland,
                modauthor_luna = 2, // mmapi_ext:npc_roster:enum_member:modauthor_luna
                modauthor_rex = 3, // mmapi_ext:npc_roster:enum_member:modauthor_rex:vacant
                LEN,
            }
            """));

        var harvest = ArchiveMarkerHarvester.Harvest(archive, LoadCatalog());

        Assert.That(harvest["npc_roster"], Is.EquivalentTo(new[] { "modauthor_luna", "modauthor_rex" }));
    }

    [Test]
    public void ShouldIgnoreMarkersOfOtherPointsAndSites()
    {
        var archive = WriteZip(("assets/gml/NpcId.gml", """
            enum NpcId {
                Ari,
                modauthor_luna = 1, // mmapi_ext:npc_roster:enum_member:modauthor_luna
                LEN,
            }
            // mmapi_ext:status_effect:enum_member:modauthor_zeal
            // mmapi_ext:npc_roster:id_to_obj:modauthor_luna
            """));

        var harvest = ArchiveMarkerHarvester.Harvest(archive, LoadCatalog());

        Assert.That(harvest["npc_roster"], Is.EquivalentTo(new[] { "modauthor_luna" }),
            "only this point's enum_member markers count, and only from this point's file");
    }

    [Test]
    public void ShouldReturnEmptyForAMissingOrCorruptArchive()
    {
        Assert.That(ArchiveMarkerHarvester.Harvest(
            Path.Combine(Path.GetTempPath(), "momi-no-such-archive.zip"), LoadCatalog()), Is.Empty);

        var notAZip = Path.Combine(
            Directory.CreateTempSubdirectory("momi-archive-harvest-test").FullName, "assets.zip");
        File.WriteAllText(notAZip, "this is not a zip");
        Assert.That(ArchiveMarkerHarvester.Harvest(notAZip, LoadCatalog()), Is.Empty);
    }

    [Test]
    public void ShouldReturnEmptyWhenTheEnumFileCarriesNoMarkers()
    {
        var archive = WriteZip(("assets/gml/NpcId.gml", """
            enum NpcId {
                Ari,
                Eiland,
                LEN,
            }
            """));

        Assert.That(ArchiveMarkerHarvester.Harvest(archive, LoadCatalog()), Is.Empty);
    }

    [Test]
    public void ShouldNotAcceptATruncatedSymbolFromAGarbledMarker()
    {
        // real markers run to end of line. A garbled one whose symbol run is
        // interrupted must not contribute the prefix as if it were the symbol
        var archive = WriteZip(("assets/gml/NpcId.gml", """
            enum NpcId {
                Ari,
                x = 1, // mmapi_ext:npc_roster:enum_member:modauthor_lu!corrupted
                y = 2, // mmapi_ext:npc_roster:enum_member:modauthor_rex trailing junk
                modauthor_luna = 3, // mmapi_ext:npc_roster:enum_member:modauthor_luna
                LEN,
            }
            """));

        var harvest = ArchiveMarkerHarvester.Harvest(archive, LoadCatalog());

        Assert.That(harvest["npc_roster"], Is.EquivalentTo(new[] { "modauthor_luna" }));
    }

    [Test]
    public void ShouldHarvestThroughTheDeclaredSiteIdRatherThanALiteral()
    {
        // a point whose enum-member site is not literally named enum_member
        // must still harvest, because the expander stamps the declared id
        const string renamedSiteCatalog = """
            version = 2

            [[extension]]
            id   = "npc_roster"
            file = "gml/NpcId.gml"

            [extension.ordinal]
            enum     = "NpcId"
            sentinel = "LEN"

            [[extension.sites]]
            id       = "member"
            kind     = "enum_member"
            template = "{{symbol}} = {{ordinal}},"
            indent   = 4

            [extension.vacancy]
            member = "{{symbol}} = {{ordinal}},"
            """ + "\n";
        var catalog = SeamCatalogLoader.Load(
            Encoding.UTF8.GetBytes(renamedSiteCatalog), "seams.toml");
        var archive = WriteZip(("assets/gml/NpcId.gml", """
            enum NpcId {
                Ari,
                modauthor_luna = 1, // mmapi_ext:npc_roster:member:modauthor_luna
                LEN,
            }
            """));

        var harvest = ArchiveMarkerHarvester.Harvest(archive, catalog);

        Assert.That(harvest["npc_roster"], Is.EquivalentTo(new[] { "modauthor_luna" }));
    }
}
