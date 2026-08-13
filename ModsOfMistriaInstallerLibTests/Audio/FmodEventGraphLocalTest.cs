using Garethp.ModsOfMistriaInstallerLib.Audio;
using Garethp.ModsOfMistriaInstallerLib.Seam;

namespace ModsOfMistriaInstallerLibTests.Audio;

// FmodEventGraph against a real vanilla bank, read-only - pure C# parsing,
// no FMOD native DLLs needed. Skipped where no pristine archive exists (CI),
// same convention as FmodBankFileLocalTest.
[TestFixture]
public class FmodEventGraphLocalTest
{
    private const uint DayBedSamples48K = 1313433; // 27.3631875s @ 48kHz
    private const uint SharedScattererSamples48K = 5952000; // 124.0s @ 48kHz

    private static string PristineZipPath()
    {
        var path = Environment.GetEnvironmentVariable("MOMI_PRISTINE_ZIP");
        if (string.IsNullOrEmpty(path) || !File.Exists(path))
            Assert.Ignore("no pristine archive (set MOMI_PRISTINE_ZIP to a disposable copy's assets zip)");
        return path!;
    }

    private static byte[] ReadFallBank()
    {
        using var pristine = new ZipPristineSource(PristineZipPath());
        var bytes = pristine.Read("assets/audio/Fall.bank");
        Assert.That(bytes, Is.Not.Null, "assets/audio/Fall.bank not found in the pristine archive");
        return bytes!;
    }

    // A plain looping single instrument: one TriggerBox.Length plus one
    // sibling TransitionRegion's Start/End - all still the same value, since
    // TransitionRegion.Start == End for a jump point rather than a real
    // range (see FmodEventGraph.ParseTransitionRegionBody).
    [Test]
    public void ShouldFindAllPlaybackLengthFieldsForALoopingAmbientTrack()
    {
        var bank = ReadFallBank();
        var offsets = FmodEventGraph.FindPlaybackLengthFieldOffsets(bank, 0, FallBankIndices.DayBed);

        Assert.That(offsets, Has.Count.EqualTo(4));
        foreach (var offset in offsets)
            Assert.That(BitConverter.ToUInt32(bank, (int)offset), Is.EqualTo(DayBedSamples48K));
    }

    // ChangingWinds belongs to only one of Fall.bank's two scatterer
    // instruments; Extended belongs to only the other. Resolving each
    // separately must not pull in the other's fields - the exact case that
    // made byte-value search unsafe (both scatterers' windows happen to
    // encode the identical original duration).
    [Test]
    public void ShouldNotCrossOverBetweenFallsTwoIndependentScatterers()
    {
        var bank = ReadFallBank();

        var changingWinds = FmodEventGraph.FindPlaybackLengthFieldOffsets(bank, 0, FallBankIndices.ChangingWinds);
        var extended = FmodEventGraph.FindPlaybackLengthFieldOffsets(bank, 0, FallBankIndices.Extended);

        Assert.That(changingWinds, Has.Count.EqualTo(4));
        Assert.That(extended, Has.Count.EqualTo(4));
        Assert.That(changingWinds.Intersect(extended), Is.Empty, "the two scatterers' windows must be distinct byte offsets");

        foreach (var offset in changingWinds.Concat(extended))
            Assert.That(BitConverter.ToUInt32(bank, (int)offset), Is.EqualTo(SharedScattererSamples48K));
    }

    // DanceOfTheLeaves and CrowsInAClearSky are each members of *both*
    // scatterers, so resolving either must return both windows' fields
    // combined (8 offsets, not 4) - this is the legitimate "shared window"
    // case, as opposed to the crossover FmodEventGraph must avoid above.
    [Test]
    public void ShouldReturnBothWindowsForATrackSharedByBothScatterers()
    {
        var bank = ReadFallBank();

        var danceOfTheLeaves = FmodEventGraph.FindPlaybackLengthFieldOffsets(bank, 0, FallBankIndices.DanceOfTheLeaves);
        var crowsInAClearSky = FmodEventGraph.FindPlaybackLengthFieldOffsets(bank, 0, FallBankIndices.CrowsInAClearSky);

        Assert.That(danceOfTheLeaves, Has.Count.EqualTo(8));
        Assert.That(crowsInAClearSky, Has.Count.EqualTo(8));
    }

    [Test]
    public void ShouldReturnEmptyForAnOutOfRangeSubsoundIndex()
    {
        var bank = ReadFallBank();
        var offsets = FmodEventGraph.FindPlaybackLengthFieldOffsets(bank, 0, subsoundIndex: 9999);
        Assert.That(offsets, Is.Empty);
    }
}
