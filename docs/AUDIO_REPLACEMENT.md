# Audio Replacement

Replace a single named track's audio inside a vanilla `.bank` file with your
own WAV - swap a song, a sound effect, an ambient loop. This replaces
existing audio; it can't add a wholly new track (see
[docs/investigations/custom-music.md](investigations/custom-music.md) for
why, and how this feature was built and proven).

## Manifest

Add a `momi/audio/*.toml` file to your mod. Each entry is keyed by the
track's own name and declares which bank it lives in and which WAV replaces
it:

```toml
# momi/audio/fall_music.toml
[snd_Fall_DanceOfTheLeaves_HidehitoIkumo]
bank = "Fall"
wav = "audio/dance_of_the_leaves.wav"
```

- **`bank`** - the bank's name without the `.bank` extension or
  `assets/audio/` prefix (e.g. `Fall`, not `Fall.bank`).
- **`wav`** - path to your replacement audio, **relative to your mod's root
  folder** (not relative to this TOML file). A mod with this manifest at
  `momi/audio/fall_music.toml` and its WAV at `audio/dance_of_the_leaves.wav`
  has both paths hanging off the same mod root, side by side - `momi/` isn't
  implied. Must be a plain PCM WAV (mono or stereo, any standard bit depth);
  export to WAV from any audio editor if you're starting from something else.

One TOML file can declare multiple tracks, in the same bank or different
ones.

## Finding a track's name and bank

There's no in-game or in-installer search yet. Use the standalone
[`tools/audio-replace`](../tools/audio-replace) CLI's `list` command against
a copy of your `assets.zip`:

```powershell
dotnet run --project tools/audio-replace -- list "<path to assets.zip>" Fall
```

Prints every track name in that bank with its format and duration - the
bracketed name (`snd_Fall_DanceOfTheLeaves_HidehitoIkumo`) is what goes in
your TOML's `[...]` heading.

## What happens at install

`AudioInstaller` runs as a normal step of the install pipeline. For each
declared replacement: it reads the *current* state of the target bank (so it
composes correctly if an earlier-installed mod already touched the same
bank), decodes every track sharing that bank's internal group - not just the
one being replaced, since the group has to be rebuilt as a whole - swaps in
your WAV, re-encodes via the real FSBank API, and writes the rebuilt bank
back. Re-encoding takes anywhere from under a second to a few seconds
depending on how many tracks share the group.

## Validation

Checked before install: `bank` is present, `wav` is present, and the WAV
file exists in your mod. Checked during install (these can't be known ahead
of time without opening the target bank): the bank exists, and a track by
that name is actually inside it. Problems are recorded per mod - a broken
entry is skipped rather than failing the whole mod's install.

## Requirements and limits

- **Windows only** for now - the underlying pipeline
  (`ModsOfMistriaInstallerLib/Audio`) P/Invokes into FMOD's Core and FSBank
  APIs, which this feature only resolves on Windows today.
- **Replacing native FMOD DLLs**: the installer needs `fmod64.dll`,
  `fsbank64.dll` and `libfsbvorbis64.dll` available. A packaged MOMI release
  bundles them (see [`tools/fmod/PACKAGING.md`](../tools/fmod/PACKAGING.md));
  a dev build needs them staged manually.
- **Replace only, not add.** Every entry must name a track that already
  exists in a vanilla bank.
- **Re-encoding is lossy.** The rebuilt group goes through Vorbis
  compression, so even an untouched sibling track in the same group is
  re-encoded (not byte-identical to vanilla) once any track in that group is
  replaced.
