using System.Text;
using Garethp.ModsOfMistriaInstallerLib.Seam;

namespace ModsOfMistriaInstallerLibTests.Seam;

// The shipped catalog against the real pristine engine source, read-only.
// Skipped where no pristine archive exists (CI). Where one does, this is the
// definitive check that every anchor and every target locator still lands.
[TestFixture]
public class ShippedCatalogLocalTest
{
    private static readonly UTF8Encoding Utf8Strict = new(false, true);

    private static string PristineZipPath()
    {
        var path = Environment.GetEnvironmentVariable("MOMI_PRISTINE_ZIP");
        if (string.IsNullOrEmpty(path) || !File.Exists(path))
            Assert.Ignore("no pristine archive (set MOMI_PRISTINE_ZIP to a disposable copy's assets zip)");
        return path!;
    }

    private static SeamCatalog LoadShippedCatalog()
    {
        var (name, bytes) = PayloadResolver.SeamCatalog();
        return SeamCatalogLoader.Load(bytes, name);
    }

    [Test]
    public void ShouldStageAgainstTheRealEngine()
    {
        var catalog = LoadShippedCatalog();
        using var pristine = new ZipPristineSource(PristineZipPath());

        var staged = SeamStager.Simulate(catalog, pristine);

        var applied = staged.Values
            .SelectMany(f => f.EntryIds)
            .Order(StringComparer.Ordinal)
            .ToList();
        Assert.That(applied, Is.EqualTo(catalog.Entries
            .Select(e => e.Id)
            .Order(StringComparer.Ordinal)
            .ToList()));
    }

    [Test]
    public void ShouldCoverEveryDirectSiteInTheRealTree()
    {
        var catalog = LoadShippedCatalog();
        Assert.That(catalog.CallRewrites, Is.Not.Empty);
        var rewrite = catalog.CallRewrites[0];
        using var pristine = new ZipPristineSource(PristineZipPath());
        var listing = pristine.GmlFiles();

        string Norm(string rel) =>
            Utf8Strict.GetString(pristine.Read(rel)!).Replace("\r\n", "\n");

        var pristineDirect = listing
            .SelectMany(rel => GmlScanner.FindCalls(Norm(rel), rewrite.Callee))
            .Count(site => site.Kind == CallKind.Call);
        Assert.That(pristineDirect, Is.GreaterThan(0));

        var staged = SeamStager.Simulate(catalog, pristine);
        SeamStager.StageCallRewrites(catalog, staged, pristine, listing);

        var rewritten = 0;
        foreach (var (rel, file) in staged)
        {
            Assert.That(GmlScanner.FindCalls(file.Text, rewrite.Callee), Is.Empty,
                $"residual callee call in {rel}");
            rewritten += GmlScanner.FindCalls(file.Text, rewrite.To).Count;
            Assert.That(GmlScanner.FindCalls(file.Text, "local_get_info"),
                Has.Count.EqualTo(GmlScanner.FindCalls(Norm(rel), "local_get_info").Count),
                $"sibling identifier touched in {rel}");
        }

        Assert.That(rewritten, Is.EqualTo(pristineDirect));

        var std = staged.GetValueOrDefault("assets/gml/scripts/GameplaySystems/Mist/Std.gml");
        Assert.That(std, Is.Not.Null);
        Assert.That(std!.Text, Does.Contain("return mmapi_local_get(string(_key));"));
    }
}
