# Audio Replacement Demo

A minimal, working example of MOMI's [audio replacement feature](../../docs/AUDIO_REPLACEMENT.md).
Copy this whole folder into your `mods/` directory and install it - one of
Fall's ambient bird chirps (`snd_fall_day_bird1_1`) plays a short original
chime instead.

## What's here

- `manifest.json` - a standard MOMI mod manifest.
- `momi/audio/replace.toml` - the one manifest entry, declaring which bank
  and track to replace and which WAV to use.
- `momi/audio/chime.wav` - a short (under a second), original three-note
  arpeggio, generated programmatically rather than sourced from anywhere -
  chosen so this example can be committed and shared freely.

## Why this track

A short SFX one-shot rather than a full music track, deliberately: it keeps
this example's WAV small, keeps the FSBank re-encode fast for anyone trying
it, and the swap is easy to reason about without needing to catch a specific
moment of gameplay - just wait for that chirp during a Fall day.

## Trying it yourself

1. Find a track and bank you want to replace instead (`tools/audio-replace`'s
   `list` command works against any bank), or keep this one.
2. Replace `momi/audio/chime.wav` with your own plain PCM WAV, and update
   `replace.toml`'s `wav` path if you rename it.
3. Install normally through MOMI.

See [docs/AUDIO_REPLACEMENT.md](../../docs/AUDIO_REPLACEMENT.md) for the full
manifest reference.
