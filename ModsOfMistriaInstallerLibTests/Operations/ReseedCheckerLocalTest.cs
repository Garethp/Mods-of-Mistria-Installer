using Garethp.ModsOfMistriaInstallerLib.Operations;
using Garethp.ModsOfMistriaInstallerLib.Seam;

namespace ModsOfMistriaInstallerLibTests.Operations;

// The reseed harvest over real saves in the game's current format, with the
// shipped catalog and a real pristine archive. Skipped where either is
// absent (CI). This is the first thing that catches a save-format change on
// a developer's machine after a game update, so it fails on any note.
[TestFixture]
public class ReseedCheckerLocalTest
{
    [Test]
    public void ShouldReadEveryRealSaveWithoutANote()
    {
        var zip = Environment.GetEnvironmentVariable("MOMI_PRISTINE_ZIP");
        var saves = Environment.GetEnvironmentVariable("MOMI_SAVES_DIR");
        if (string.IsNullOrEmpty(zip) || !File.Exists(zip))
            Assert.Ignore("no pristine archive (set MOMI_PRISTINE_ZIP to a disposable copy's assets zip)");
        if (string.IsNullOrEmpty(saves) || !Directory.Exists(saves))
            Assert.Ignore("no saves folder (set MOMI_SAVES_DIR to the game's saves folder)");

        ReseedCheckResult result;
        using (var pristine = new ZipPristineSource(zip!))
        {
            result = ReseedChecker.Check(saves!, pristine);
        }

        TestContext.WriteLine(ReseedChecker.RenderText(result, saves!, zip!, null));
        Assert.That(result.SaveCount, Is.GreaterThan(0), "the saves folder holds no game-*.sav files");
        Assert.That(result.Notes, Is.Empty, string.Join("\n", result.Notes));
    }
}
