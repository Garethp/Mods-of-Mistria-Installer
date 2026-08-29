using Garethp.ModsOfMistriaInstallerLib.Generator;
using Garethp.ModsOfMistriaInstallerLib.Models.MOMI;
using ModsOfMistriaInstallerLibTests.Fixtures;

namespace ModsOfMistriaInstallerLibTests.Models.MOMI;

[TestFixture]
public class AudioReplacementFileTest
{
    private static MockMod ModWithWav() => new(new Dictionary<string, object>
    {
        { "changing_winds.wav", new byte[] { 1, 2, 3 } },
    });

    [Test]
    public void ShouldErrorWhenBankIsMissing()
    {
        var entry = new AudioReplacementFile { Id = "my_track", Wav = "changing_winds.wav" };
        var validation = entry.Validate(new Validation(), ModWithWav(), "momi/audio/test.toml");

        Assert.That(validation.Status, Is.EqualTo(ValidationStatus.Invalid));
        Assert.That(validation.Errors.Single().Message, Does.Contain("my_track").And.Contain("bank"));
    }

    [Test]
    public void ShouldErrorWhenWavIsMissing()
    {
        var entry = new AudioReplacementFile { Id = "my_track", Bank = "Fall" };
        var validation = entry.Validate(new Validation(), ModWithWav(), "momi/audio/test.toml");

        Assert.That(validation.Status, Is.EqualTo(ValidationStatus.Invalid));
        Assert.That(validation.Errors.Single().Message, Does.Contain("my_track").And.Contain("wav"));
    }

    [Test]
    public void ShouldErrorWhenWavFileDoesNotExist()
    {
        var entry = new AudioReplacementFile { Id = "my_track", Bank = "Fall", Wav = "does_not_exist.wav" };
        var validation = entry.Validate(new Validation(), ModWithWav(), "momi/audio/test.toml");

        Assert.That(validation.Status, Is.EqualTo(ValidationStatus.Invalid));
        Assert.That(validation.Errors.Single().Message, Does.Contain("does_not_exist.wav"));
    }

    [Test]
    public void ShouldBeValidWhenBankAndExistingWavAreGiven()
    {
        var entry = new AudioReplacementFile { Id = "my_track", Bank = "Fall", Wav = "changing_winds.wav" };
        var validation = entry.Validate(new Validation(), ModWithWav(), "momi/audio/test.toml");

        Assert.That(validation.Status, Is.EqualTo(ValidationStatus.Valid));
    }
}
