using Garethp.ModsOfMistriaInstallerLib.Generator;
using Garethp.ModsOfMistriaInstallerLib.Lang;
using Garethp.ModsOfMistriaInstallerLib.ModTypes;
using Tomlyn.Serialization;

namespace Garethp.ModsOfMistriaInstallerLib.Models.MOMI;

// One momi/audio/*.toml entry: replace a single named track's audio inside
// a vanilla .bank file with a mod's own WAV. Keyed by the track's own name
// (e.g. "snd_Fall_DanceOfTheLeaves_HidehitoIkumo") - find it with
// `audio-replace list <assets.zip> <bank>` (tools/audio-replace).
public class AudioReplacementFile
{
    public string Id = "";

    [TomlPropertyName("bank")]
    public string? Bank { get; set; }

    [TomlPropertyName("wav")]
    public string? Wav { get; set; }

    public Validation Validate(Validation validation, IMod mod, string file)
    {
        if (string.IsNullOrWhiteSpace(Bank))
            validation.AddError(mod, file, string.Format(Resources.CoreErrorAudioNoBank, Id));

        if (string.IsNullOrWhiteSpace(Wav))
            validation.AddError(mod, file, string.Format(Resources.CoreErrorAudioNoWav, Id));
        else if (!mod.FileExists(Wav))
            validation.AddError(mod, file, string.Format(Resources.CoreErrorAudioWavMissing, Id, Wav));

        return validation;
    }
}
