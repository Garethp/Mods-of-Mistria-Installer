namespace ModsOfMistriaInstallerLibTests.Audio;

// Subsound indices within the real Fall.bank's group 0, determined once via
// FmodCoreNative.DecodeGroup against a pristine assets.bak.zip and hardcoded
// here so fixtures that only need FmodEventGraph (pure C#, no FMOD native
// DLLs) aren't forced to also require MOMI_FMOD_NATIVE_DIR just to look a
// name up. Shared across Audio/FmodEventGraphLocalTest.cs,
// Audio/FmodBankFileLocalTest.cs and Installer/AudioInstallerLocalTest.cs so
// the same real-file facts aren't duplicated (and risk drifting) three times.
internal static class FallBankIndices
{
    public const int ChangingWinds = 10; // snd_Fall_ChangingWinds_HidehitoIkumo, ~120.0s decoded, one of two Fall music scatterers
    public const int Extended = 16; // "Fall - Changing Winds (Extended)"_HidehitoIkumo, ~247.98s decoded, the *other* scatterer only
    public const int DanceOfTheLeaves = 17; // snd_Fall_DanceOfTheLeaves_HidehitoIkumo, member of both scatterers
    public const int CrowsInAClearSky = 26; // snd_Fall_CrowsInAClearSky_HidehitoIkumo, member of both scatterers
    public const int NightBed = 1; // snd_fall_night_bed, a plain looping single instrument (~33.77s)
    public const int DayBed = 14; // snd_fall_day_bed, a plain looping single instrument (~27.36s)
}
