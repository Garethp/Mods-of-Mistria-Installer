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
ones - just add another `[...]` entry:

```toml
# momi/audio/fall_music.toml
[snd_Fall_ChangingWinds_HidehitoIkumo]
bank = "Fall"
wav = "audio/my_song.wav"

[snd_Fall_DanceOfTheLeaves_HidehitoIkumo]
bank = "Fall"
wav = "audio/my_song.wav"

[snd_Fall_CrowsInAClearSky_HidehitoIkumo]
bank = "Fall"
wav = "audio/my_song.wav"

["Fall - Changing Winds (Extended)_HidehitoIkumo"]
bank = "Fall"
wav = "audio/my_song.wav"
```

Worth doing for background music specifically: the game doesn't always play
the same track for a given season - `Music/Playlists/Fall` is an FMOD
playlist that picks randomly among these four each time, and that pick
happens inside FMOD itself, invisible to and uncontrollable from GML or this
feature. Replacing only one means you might not hear your swap for a while;
replacing every track in the pool guarantees you always do. (The bracketed
key needs quotes here only because this one track name contains spaces -
see [Audio Tracks by Bank](AUDIO_TRACKS.md) for exact names.)

## Finding a track's name and bank

[**Audio Tracks by Bank**](AUDIO_TRACKS.md) lists every track name in every
bank as of this doc's writing - check there first. It's a snapshot, though,
and can drift as the game updates: if a track isn't there or looks wrong,
regenerate it yourself with the standalone
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
that name is actually inside it. A third category is checked only if it
happens: the native FMOD/FSBank layer itself failing (missing DLLs, a
corrupt bank, an encoder error) - verified against a real published build,
not just reasoned about (see
[docs/investigations/custom-music.md](investigations/custom-music.md)'s
"Verified against a real published build" section).

All three are recorded as validation errors, never left to crash anything -
a broken entry is skipped, the rest of that mod (and every other mod)
installs normally. **The CLI doesn't currently print these after an install
finishes** (a pre-existing gap, not specific to audio replacement - see
[docs/investigations/custom-music.md](investigations/custom-music.md)'s
"Phase 2 built and proven" section) - if a track you expected to change
didn't, that's the first thing to suspect, not a sign nothing happened.

## Requirements and limits

- **Windows only** for now - the underlying pipeline
  (`ModsOfMistriaInstallerLib/Audio`) P/Invokes into FMOD's Core and FSBank
  APIs, which this feature only resolves on Windows today.
- **Needs native FMOD DLLs**: `fmod64.dll`, `fsbank64.dll` and
  `libfsbvorbis64.dll`. A packaged MOMI release bundles them (see
  [`tools/fmod/PACKAGING.md`](../tools/fmod/PACKAGING.md)); a dev build needs
  them staged manually. If they're missing, this feature's replacements are
  skipped (recorded as validation errors) - it doesn't break anything else a
  mod or install is doing.
- **Replace only, not add.** Every entry must name a track that already
  exists in a vanilla bank.
- **Re-encoding is lossy.** The rebuilt group goes through Vorbis
  compression, so even an untouched sibling track in the same group is
  re-encoded (not byte-identical to vanilla) once any track in that group is
  replaced.
- **A replacement meaningfully longer than the track it replaces will not
  play to completion.** Confirmed on both constructs tested, and confirmed
  the root cause via FMOD's own Studio API, not just observed: a playlist
  track (e.g. a season's background music) gets cut short and the playlist
  advances early; a looping ambient track gets cut short and loops from the
  start early. Both because the compiled FMOD event carries a fixed
  timeline length set when the event was authored - independent of
  whatever audio a bank actually contains, and outside anything this
  feature reads or writes, so there's no way to change it short of the
  original FMOD Studio project source. Replacements close to the original
  track's length are unaffected; see
  [docs/investigations/custom-music.md](investigations/custom-music.md)'s "A
  new limit" section for the full investigation.
