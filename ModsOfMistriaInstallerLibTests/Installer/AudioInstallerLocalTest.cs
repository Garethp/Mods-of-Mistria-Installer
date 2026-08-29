using System.IO.Compression;
using System.Runtime.InteropServices;
using Garethp.ModsOfMistriaInstallerLib;
using Garethp.ModsOfMistriaInstallerLib.Audio;
using Garethp.ModsOfMistriaInstallerLib.Generator;
using Garethp.ModsOfMistriaInstallerLib.Installer;
using Garethp.ModsOfMistriaInstallerLib.Utils;
using ModsOfMistriaInstallerLibTests.Audio;
using ModsOfMistriaInstallerLibTests.Fixtures;

namespace ModsOfMistriaInstallerLibTests.Installer;

// AudioInstaller against a real bank inside a real zip, using the real
// fmod64/fsbank64/libfsbvorbis64 DLLs - the actual mod-facing install path,
// not just the underlying Audio/ pipeline already proven by
// ModsOfMistriaInstallerLibTests/Audio/*LocalTest.cs. Skipped unless both
// are available locally - CI has neither, since FMOD's SDK is proprietary.
[TestFixture]
public class AudioInstallerLocalTest
{
    private string? _workingZipPath;

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

        NativeLibrary.Load(Path.Combine(dir!, "libfsbvorbis64.dll"));
        FmodNativeTestSupport.EnsureResolver(dir!);
    }

    [SetUp]
    public void CopyPristineZip()
    {
        var zipPath = Environment.GetEnvironmentVariable("MOMI_PRISTINE_ZIP");
        if (string.IsNullOrEmpty(zipPath) || !File.Exists(zipPath))
            Assert.Ignore("no pristine archive (set MOMI_PRISTINE_ZIP to a disposable copy's assets zip)");

        _workingZipPath = Path.Combine(Path.GetTempPath(), $"AudioInstallerLocalTest_{Guid.NewGuid()}.zip");
        File.Copy(zipPath!, _workingZipPath);
    }

    [TearDown]
    public void CleanUpWorkingZip()
    {
        if (_workingZipPath is not null && File.Exists(_workingZipPath))
            File.Delete(_workingZipPath);
    }

    // Deliberately a different format (mono, 8kHz) from every real track in
    // Fall.bank (all stereo, 48kHz) - the format mismatch after install is
    // proof the swap landed, not just a byte-length check.
    private static byte[] TinyWav() =>
        FmodCoreNative.ToWav(new FmodCoreNative.DecodedSubsound("replacement", 8000, 1, 16, new byte[800]));

    [Test]
    public void ShouldReplaceOneTrackAndLeaveOthersUntouched()
    {
        using var archive = ZipFile.Open(_workingZipPath!, ZipArchiveMode.Update);
        var fileModifier = new ZipFileModifier(archive);

        var mod = new MockMod(new Dictionary<string, object>
        {
            {
                "momi/audio/test.toml",
                """
                [snd_fall_day_bird1_1]
                bank = "Fall"
                wav = "replacement.wav"
                """
            },
            { "replacement.wav", TinyWav() },
        });

        new AudioInstaller(new Dictionary<string, string>(), fileModifier)
            .Install(mod, new GeneratedInformation(), (_, _) => { });

        Assert.That(mod.GetValidation().Errors, Is.Empty);

        var bank = ReadBank(fileModifier);
        var decoded = FmodCoreNative.DecodeGroup(FmodBankFile.ExtractGroup(bank, 0));

        var replaced = decoded.Single(s => s.Name == "snd_fall_day_bird1_1");
        Assert.That(replaced.SampleRate, Is.EqualTo(8000));
        Assert.That(replaced.Channels, Is.EqualTo(1));

        var untouched = decoded.Single(s => s.Name == "snd_fall_day_bird1_2");
        Assert.That(untouched.SampleRate, Is.EqualTo(48000));
        Assert.That(untouched.Channels, Is.EqualTo(2));
    }

    [Test]
    public void ShouldErrorWhenTheTrackDoesNotExistInTheBank()
    {
        using var archive = ZipFile.Open(_workingZipPath!, ZipArchiveMode.Update);
        var fileModifier = new ZipFileModifier(archive);

        var mod = new MockMod(new Dictionary<string, object>
        {
            {
                "momi/audio/test.toml",
                """
                [track_that_does_not_exist]
                bank = "Fall"
                wav = "replacement.wav"
                """
            },
            { "replacement.wav", TinyWav() },
        });

        new AudioInstaller(new Dictionary<string, string>(), fileModifier)
            .Install(mod, new GeneratedInformation(), (_, _) => { });

        Assert.That(mod.GetValidation().Status, Is.EqualTo(ValidationStatus.Invalid));
        Assert.That(mod.GetValidation().Errors.Single().Message, Does.Contain("track_that_does_not_exist"));
    }

    [Test]
    public void ShouldErrorWhenTheBankDoesNotExist()
    {
        using var archive = ZipFile.Open(_workingZipPath!, ZipArchiveMode.Update);
        var fileModifier = new ZipFileModifier(archive);

        var mod = new MockMod(new Dictionary<string, object>
        {
            {
                "momi/audio/test.toml",
                """
                [some_track]
                bank = "NotARealBank"
                wav = "replacement.wav"
                """
            },
            { "replacement.wav", TinyWav() },
        });

        new AudioInstaller(new Dictionary<string, string>(), fileModifier)
            .Install(mod, new GeneratedInformation(), (_, _) => { });

        Assert.That(mod.GetValidation().Status, Is.EqualTo(ValidationStatus.Invalid));
        Assert.That(mod.GetValidation().Errors.Single().Message, Does.Contain("NotARealBank"));
    }

    // Deliberately a plain-silence WAV whose duration differs from every
    // real track in Fall.bank, so that a changed playback-length field is
    // proof the patch landed, not a coincidental match. Kept short to keep
    // FSBank's own encode step fast, in either direction (shorter or longer
    // than the original) - what matters for this test is that it differs.
    private static byte[] ShortSilentWav(double seconds) =>
        FmodCoreNative.ToWav(new FmodCoreNative.DecodedSubsound(
            "replacement", 48000, 2, 16, new byte[(int)(seconds * 48000) * 2 * 2]));

    // The real regression case this whole GUID-anchored rewrite exists for:
    // replacing a track that lives in one of Fall.bank's two scatterer
    // instruments must update that scatterer's own TriggerBox/TransitionRegion
    // fields so playback doesn't cut off or loop at the *original* track's
    // duration, and must leave the *other* scatterer (Extended's, which
    // ChangingWinds isn't a member of) completely untouched. It must also
    // push out the scatterer's own SpawnTime - real playback tracing showed
    // that patching only the outer timeline still let the scatterer spawn a
    // second, overlapping voice on its own original ~150-180s schedule,
    // regardless of whether the replacement was still playing.
    [Test]
    public void ShouldExtendPlaybackLengthFieldsForAReplacedMusicTrackAndLeaveTheOtherScattererAlone()
    {
        using var archive = ZipFile.Open(_workingZipPath!, ZipArchiveMode.Update);
        var fileModifier = new ZipFileModifier(archive);

        var mod = new MockMod(new Dictionary<string, object>
        {
            {
                "momi/audio/test.toml",
                """
                [snd_Fall_ChangingWinds_HidehitoIkumo]
                bank = "Fall"
                wav = "replacement.wav"
                """
            },
            { "replacement.wav", ShortSilentWav(3.0) },
        });

        new AudioInstaller(new Dictionary<string, string>(), fileModifier)
            .Install(mod, new GeneratedInformation(), (_, _) => { });

        Assert.That(mod.GetValidation().Errors, Is.Empty);

        var bank = ReadBank(fileModifier);
        const uint expectedSamples48K = (uint)(3.0 * 48000);

        var changingWindsOffsets = FmodEventGraph.FindPlaybackLengthFieldOffsets(
            bank, 0, ModsOfMistriaInstallerLibTests.Audio.FallBankIndices.ChangingWinds);
        Assert.That(changingWindsOffsets, Is.Not.Empty);
        foreach (var offset in changingWindsOffsets)
            Assert.That(BitConverter.ToUInt32(bank, (int)offset), Is.EqualTo(expectedSamples48K));

        var extendedOffsets = FmodEventGraph.FindPlaybackLengthFieldOffsets(
            bank, 0, ModsOfMistriaInstallerLibTests.Audio.FallBankIndices.Extended);
        foreach (var offset in extendedOffsets)
            Assert.That(BitConverter.ToUInt32(bank, (int)offset), Is.Not.EqualTo(expectedSamples48K));

        // SpawnTime is set to the new duration *plus* a buffer, not the same
        // instant as the outer window - see AudioInstaller.InstallBank's own
        // comment on spawnTimeBufferSeconds for why setting them equal
        // reproduces vanilla's ordering backwards (both boundaries landing
        // at once, discovered via real in-game testing - 0s overlapped,
        // 30s and then 5s were both confirmed clean, 5s is what shipped).
        var spawnTimeOffsets = FmodEventGraph.FindScattererSpawnTimeOffsets(
            bank, 0, ModsOfMistriaInstallerLibTests.Audio.FallBankIndices.ChangingWinds);
        Assert.That(spawnTimeOffsets, Has.Count.EqualTo(2));
        foreach (var offset in spawnTimeOffsets)
            Assert.That(BitConverter.ToSingle(bank, (int)offset), Is.EqualTo(8.0f));
    }

    private static byte[] ReadBank(ZipFileModifier fileModifier)
    {
        using var stream = fileModifier.GetReadStream("assets/audio/Fall.bank");
        using var mem = new MemoryStream();
        stream.CopyTo(mem);
        return mem.ToArray();
    }
}
