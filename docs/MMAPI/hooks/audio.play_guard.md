# Hook: audio.play_guard

Block any sound effect before it plays.

`audio.play_guard` is a **guard** hook. Register a callback with `mmapi_guard`. See [Hooks](../HOOKS.md) for how registration and dispatch work.

## Contract

Fires at the top of `TANGO.play(asset_name, ...)`, before a sound effect starts. Return `false` to suppress the sound (`play` returns `undefined`). `undefined` or `true` allows. Runs for every SFX the game plays through TANGO, so keep handlers cheap.

| | |
| --- | --- |
| **Fires** | At the top of `TANGO.play(asset_name, ...)`, before a sound effect starts. |
| **ctx** | `{ asset_name }` |
| **Kind contract** | The callback returns `false` to veto the action. `undefined` or `true` allows it. Guards fail open: a callback that throws counts as allow. |

### The ctx struct

- `asset_name` - the sound's TANGO asset path, exactly as the engine passed it to `play()`, e.g. `"SoundEffects/Entrances/LadderDescend"`.

> [!IMPORTANT]
> Hot path. This guard runs for every sound effect the game plays. Make the callback's first check its cheapest early-exit.

## Usage

```gml
// audio.play_guard is a GUARD: return false to block it, undefined (or true)
// to allow. Guards fail OPEN - if your handler crashes, the action happens.
function quiet_farm_audio_play_guard(_ctx) {
    // _ctx is { asset_name }.
    //   .asset_name - the sound's TANGO asset path.
    // HOT PATH: fires for every sound effect the game plays. Make your first
    // check the cheapest one and get out early when your mod has nothing to do.
    if (!__quiet_farm_runtime().enabled) return undefined;
    // if (<your condition>) {
    //     return false; // veto - the engine then runs: return undefined;
    // }
    return undefined; // allow everything else
}

// inside your latched register function (see Mod Anatomy):
mmapi_guard("audio.play_guard", quiet_farm_audio_play_guard);
```

## Engine Wiring

- Seam [`audio_play_guard`](../seams/audio_play_guard.md) dispatches from `gml/scripts/Tango.gml`, at the head of `play()`. On veto the engine runs `return undefined;`.

## See Also

- [audio.music_selector](audio.music_selector.md) - Swap the dungeon biome music track.
