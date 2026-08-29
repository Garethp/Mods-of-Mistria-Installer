using Garethp.ModsOfMistriaInstallerLib.Audio;
using Garethp.ModsOfMistriaInstallerLib.Seam;

namespace ModsOfMistriaInstallerLibTests.Audio;

// FmodCoreNative against the real fmod64.dll and a real bank's FSB5 bytes.
// Skipped unless both a native DLL folder and a pristine archive are
// available locally - CI has neither, since FMOD's SDK is proprietary and
// not committed to the repo.
[TestFixture]
public class FmodCoreNativeLocalTest
{
    [OneTimeSetUp]
    public void SetUpNativeResolver()
    {
        var dir = FmodNativeTestSupport.NativeDirOrNull("fmod64.dll");
        if (dir is null)
            Assert.Ignore("no local FMOD native DLLs (set MOMI_FMOD_NATIVE_DIR to a folder containing fmod64.dll)");

        FmodNativeTestSupport.EnsureResolver(dir);
    }

    private static byte[] ReadFallGroup0()
    {
        var zipPath = Environment.GetEnvironmentVariable("MOMI_PRISTINE_ZIP");
        if (string.IsNullOrEmpty(zipPath) || !File.Exists(zipPath))
            Assert.Ignore("no pristine archive (set MOMI_PRISTINE_ZIP to a disposable copy's assets zip)");

        using var pristine = new ZipPristineSource(zipPath!);
        var bank = pristine.Read("assets/audio/Fall.bank");
        Assert.That(bank, Is.Not.Null, "assets/audio/Fall.bank not found in the pristine archive");
        return FmodBankFile.ExtractGroup(bank!, 0);
    }

    [Test]
    public void ShouldDecodeRealSubsoundsWithSaneFormats()
    {
        var group0 = ReadFallGroup0();
        var subsounds = FmodCoreNative.DecodeGroup(group0);

        Assert.That(subsounds, Is.Not.Empty);
        foreach (var s in subsounds)
        {
            Assert.That(s.Name, Is.Not.Empty);
            Assert.That(s.SampleRate, Is.GreaterThan(0));
            Assert.That(s.Channels, Is.GreaterThan(0));
            Assert.That(s.Bits, Is.GreaterThan(0));
            Assert.That(s.Pcm, Is.Not.Empty);
        }
    }

    // Dumped to disk for a manual listen - the real proof the decode path
    // is correct, same convention as FmodBankFileLocalTest's round-trip dump.
    [Test]
    public void ShouldProduceListenableWavFiles()
    {
        var group0 = ReadFallGroup0();
        var subsounds = FmodCoreNative.DecodeGroup(group0);

        var dumpDir = Path.Combine(Path.GetTempPath(), "FmodCoreNativeLocalTest_Fall_decoded");
        Directory.CreateDirectory(dumpDir);
        foreach (var s in subsounds)
            File.WriteAllBytes(Path.Combine(dumpDir, $"{s.Name}.wav"), FmodCoreNative.ToWav(s));

        TestContext.Out.WriteLine($"Decoded WAVs written to: {dumpDir}");
        Assert.That(Directory.GetFiles(dumpDir, "*.wav"), Has.Length.EqualTo(subsounds.Count));
    }
}
