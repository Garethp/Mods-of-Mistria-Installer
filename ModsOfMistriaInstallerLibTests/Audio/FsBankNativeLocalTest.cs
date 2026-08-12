using System.Runtime.InteropServices;
using Garethp.ModsOfMistriaInstallerLib.Audio;
using Garethp.ModsOfMistriaInstallerLib.Seam;

namespace ModsOfMistriaInstallerLibTests.Audio;

// FsBankNative against the real fsbank64.dll/libfsbvorbis64.dll and a real
// bank's decoded subsounds - the full decode -> encode round trip, not just
// the identity container round trip FmodBankFileLocalTest already proves.
// Skipped unless both native DLLs and a pristine archive are available
// locally - CI has neither, since FMOD's SDK is proprietary.
//
// Uses MinesUpper.bank rather than Fall.bank: its one group holds three
// short SFX barks instead of Fall.bank's ~30 subsounds including full
// music tracks, so a real Vorbis encode pass stays fast here.
[TestFixture]
public class FsBankNativeLocalTest
{
    [OneTimeSetUp]
    public void SetUpNativeResolver()
    {
        var dir = FmodNativeTestSupport.NativeDirOrNull("fmod64.dll", "fsbank64.dll", "libfsbvorbis64.dll");
        if (dir is null)
        {
            Assert.Ignore(
                "no local FMOD native DLLs (set MOMI_FMOD_NATIVE_DIR to a folder containing " +
                "fmod64.dll, fsbank64.dll and libfsbvorbis64.dll)");
        }

        // libfsbvorbis64 is fsbank64's own Vorbis encoder plugin - loaded by
        // fsbank64 itself, not P/Invoked here, so it just needs to be
        // resolvable. Preloading it guarantees that regardless of process
        // PATH/working directory. Unlike SetDllImportResolver, this is safe
        // to call every run - no shared once-only guard needed.
        NativeLibrary.Load(Path.Combine(dir!, "libfsbvorbis64.dll"));
        FmodNativeTestSupport.EnsureResolver(dir!);
    }

    private static byte[] ReadMinesUpperGroup0()
    {
        var zipPath = Environment.GetEnvironmentVariable("MOMI_PRISTINE_ZIP");
        if (string.IsNullOrEmpty(zipPath) || !File.Exists(zipPath))
            Assert.Ignore("no pristine archive (set MOMI_PRISTINE_ZIP to a disposable copy's assets zip)");

        using var pristine = new ZipPristineSource(zipPath!);
        var bank = pristine.Read("assets/audio/MinesUpper.bank");
        Assert.That(bank, Is.Not.Null, "assets/audio/MinesUpper.bank not found in the pristine archive");
        return FmodBankFile.ExtractGroup(bank!, 0);
    }

    [Test]
    public void ShouldReEncodeDecodedSubsoundsIntoAPlayableGroup()
    {
        var original = ReadMinesUpperGroup0();
        var decoded = FmodCoreNative.DecodeGroup(original);
        Assert.That(decoded, Is.Not.Empty);

        var rebuiltFsb = FsBankNative.EncodeGroup(decoded);

        Assert.That(rebuiltFsb, Has.Length.GreaterThan(4));
        var magic = System.Text.Encoding.ASCII.GetString(rebuiltFsb, 0, 4);
        Assert.That(magic, Is.EqualTo("FSB5"), "FSBank_Build did not produce a valid FSB5 blob");

        // The real proof this half of the pipeline works: FMOD must be able
        // to load the group FSBank just produced and see the same subsound
        // set back out of it - not byte-identical (Vorbis is lossy and the
        // original bank may use a different format/quality), but structurally
        // sound and round-trippable through the same decode path.
        var redecoded = FmodCoreNative.DecodeGroup(rebuiltFsb);
        Assert.That(redecoded, Has.Count.EqualTo(decoded.Count));
        for (var i = 0; i < decoded.Count; i++)
        {
            Assert.That(redecoded[i].Name, Is.EqualTo(decoded[i].Name));
            Assert.That(redecoded[i].Channels, Is.EqualTo(decoded[i].Channels));
            Assert.That(redecoded[i].Pcm, Is.Not.Empty);
        }
    }

    // Dumped for a manual in-bank / in-game listen test, same convention as
    // FmodBankFileLocalTest's and FmodCoreNativeLocalTest's dumps.
    [Test]
    public void ShouldRebuildAFullBankUsingTheReEncodedGroup()
    {
        var zipPath = Environment.GetEnvironmentVariable("MOMI_PRISTINE_ZIP");
        if (string.IsNullOrEmpty(zipPath) || !File.Exists(zipPath))
            Assert.Ignore("no pristine archive (set MOMI_PRISTINE_ZIP to a disposable copy's assets zip)");

        using var pristine = new ZipPristineSource(zipPath!);
        var bank = pristine.Read("assets/audio/MinesUpper.bank");
        Assert.That(bank, Is.Not.Null, "assets/audio/MinesUpper.bank not found in the pristine archive");

        var group0 = FmodBankFile.ExtractGroup(bank!, 0);
        var decoded = FmodCoreNative.DecodeGroup(group0);
        var rebuiltFsb = FsBankNative.EncodeGroup(decoded);

        var rebuiltBank = FmodBankFile.ReplaceGroup(bank!, 0, rebuiltFsb);

        var dumpPath = Path.Combine(Path.GetTempPath(), "FsBankNativeLocalTest_MinesUpper_reencoded.bank");
        File.WriteAllBytes(dumpPath, rebuiltBank);
        TestContext.Out.WriteLine($"Re-encoded bank written to: {dumpPath}");

        Assert.That(FmodBankFile.ReadGroups(rebuiltBank), Has.Count.EqualTo(FmodBankFile.ReadGroups(bank!).Count));
    }
}
