using Garethp.ModsOfMistriaInstallerLib.Audio;
using Garethp.ModsOfMistriaInstallerLib.Seam;

namespace ModsOfMistriaInstallerLibTests.Audio;

// FmodBankFile against a real vanilla bank, read-only. Skipped where no
// pristine archive exists (CI), same convention as ShippedCatalogLocalTest.
[TestFixture]
public class FmodBankFileLocalTest
{
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

    [Test]
    public void ShouldReadTheRealGroupTable()
    {
        var bank = ReadFallBank();
        var groups = FmodBankFile.ReadGroups(bank);

        Assert.That(groups, Is.Not.Empty);
        foreach (var group in groups)
        {
            Assert.That(group.Offset, Is.GreaterThan(0));
            Assert.That(group.Size, Is.GreaterThan(0));
            Assert.That(group.Offset + group.Size, Is.LessThanOrEqualTo((uint)bank.Length));
        }
    }

    [Test]
    public void ShouldExtractGroupBytesMatchingFsb5Magic()
    {
        var bank = ReadFallBank();
        var group0 = FmodBankFile.ExtractGroup(bank, 0);

        Assert.That(group0, Has.Length.GreaterThan(4));
        var magic = System.Text.Encoding.ASCII.GetString(group0, 0, 4);
        Assert.That(magic, Is.EqualTo("FSB5"));
    }

    // Replacing a group with its own unchanged bytes must reproduce the
    // original file exactly - not just an equivalent-looking bank, the
    // literal same bytes. Confirmed against the real Fall.bank: it does.
    [Test]
    public void ShouldRoundTripGroup0ByteForByte()
    {
        var bank = ReadFallBank();
        var group0 = FmodBankFile.ExtractGroup(bank, 0);

        var rebuilt = FmodBankFile.ReplaceGroup(bank, 0, group0);

        Assert.That(rebuilt, Is.EqualTo(bank));
    }

    // Patching one track's playback-length fields must not touch a sibling's
    // - the exact failure mode a byte-value-search approach had (see
    // FmodEventGraph's own header comment): two of Fall.bank's tracks
    // (ChangingWinds, Extended) happen to share the identical original
    // duration despite belonging to different scatterer windows.
    [Test]
    public void ShouldPatchOnlyTheTargetedTracksFields()
    {
        var bank = ReadFallBank();

        var patched = FmodBankFile.PatchPlaybackLengthFields(bank, 0, FallBankIndices.DayBed, newSamples48K: 999_000);

        var dayBedOffsets = FmodEventGraph.FindPlaybackLengthFieldOffsets(patched, 0, FallBankIndices.DayBed);
        Assert.That(dayBedOffsets, Has.Count.EqualTo(4));
        foreach (var offset in dayBedOffsets)
            Assert.That(BitConverter.ToUInt32(patched, (int)offset), Is.EqualTo(999_000));

        var nightBedOffsets = FmodEventGraph.FindPlaybackLengthFieldOffsets(patched, 0, FallBankIndices.NightBed);
        foreach (var offset in nightBedOffsets)
            Assert.That(BitConverter.ToUInt32(patched, (int)offset), Is.Not.EqualTo(999_000));
    }

    // A subsound with nothing referencing it on any timeline is a no-op, not
    // an error - the same guarantee AudioInstaller relies on for tracks that
    // simply aren't wrapped in a timeline construct.
    [Test]
    public void ShouldReturnTheSameBankWhenNothingReferencesTheSubsound()
    {
        var bank = ReadFallBank();
        var patched = FmodBankFile.PatchPlaybackLengthFields(bank, 0, subsoundIndex: 9999, newSamples48K: 999_000);
        Assert.That(patched, Is.EqualTo(bank));
    }
}
