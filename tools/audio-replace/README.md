# audio-replace

Swaps one music/SFX track inside a Fields of Mistria `.bank` file for your
own WAV, using MOMI's own audio pipeline
([`ModsOfMistriaInstallerLib/Audio/`](../../ModsOfMistriaInstallerLib/Audio))
instead of any third-party tool. See
[docs/investigations/custom-music.md](../../docs/investigations/custom-music.md)
for how this was discovered and proven.

There's now a real mod format for this - [`momi/audio/*.toml`](../../docs/AUDIO_REPLACEMENT.md),
installed automatically by `AudioInstaller` like any other MOMI mod content.
Use that for an actual mod. This tool is what you reach for outside that:
finding a track's name (step 2 below powers
[docs/AUDIO_TRACKS.md](../../docs/AUDIO_TRACKS.md)'s catalog), or patching a
zip directly without going through the mod-install pipeline at all.

## 1. Get the FMOD native DLLs

You need `fmod64.dll`, `fsbank64.dll` and `libfsbvorbis64.dll` - these come
from the FMOD Engine SDK (needs a free FMOD account) or are bundled inside
any release of [Fmod-Bank-Tools](https://github.com/Wouldubeinta/Fmod-Bank-Tools).
They are proprietary, so they are not checked into this repo.

Put all three in one folder and point an environment variable at it:

```powershell
$env:MOMI_FMOD_NATIVE_DIR = "C:\path\to\folder\with\the\three\dlls"
```

## 2. Find the bank and track name you want to replace

Check [docs/AUDIO_TRACKS.md](../../docs/AUDIO_TRACKS.md) first - it's a
generated snapshot of every track in every bank. If it's missing something
or looks stale, regenerate it yourself: every `.bank` file lives at
`assets/audio/<Name>.bank` inside `assets.zip`, and if you don't already
know which bank has the track you want, you'll need to check each candidate
- there's no game-wide search yet.

```powershell
dotnet run --project tools/audio-replace -- list "<path to assets.zip>" Fall
```

Prints every track name in that bank plus its format and duration, e.g.:

```
[group 0] snd_Fall_DanceOfTheLeaves_HidehitoIkumo  (48000Hz 2ch 16bit, 113.5s)
```

## 3. Replace the track

Your replacement must be a plain PCM WAV (mono or stereo, any standard bit
depth) - export to WAV from any audio editor if you're starting from
something else.

```powershell
dotnet run --project tools/audio-replace -- replace `
  "<path to assets.zip>" `
  Fall `
  snd_Fall_DanceOfTheLeaves_HidehitoIkumo `
  "my_song.wav" `
  "assets.zip.patched"
```

The input zip is never touched - this always writes a new file (here,
`assets.zip.patched`) alongside it. Re-encoding a full bank takes a few
seconds to a couple of minutes depending on how many tracks share its group.

## 4. Install it and test

1. Close the game.
2. **Back up your real `assets.zip`** (copy it somewhere safe) - if this
   goes wrong, this is how you recover.
3. Copy `assets.zip.patched` over the game's `assets.zip`.
4. Launch the game and check the track plays correctly.
5. If anything looks wrong (or you're done testing), restore the backup
   from step 2.

There's no undo beyond that backup, so don't skip step 2.
