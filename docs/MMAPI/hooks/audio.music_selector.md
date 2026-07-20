# Hook: audio.music_selector

Swap the dungeon biome music track.

`audio.music_selector` is a **filter** hook. Register a callback with `mmapi_filter`. See [Hooks](../HOOKS.md) for how registration and dispatch work.

## Contract

Fires in the scene music selector when the dungeon biome track is chosen (standard dungeon rooms, and only when the runner is not blocking music). The filtered value is the biome music track. ctx is `undefined`. Return the replacement track, or `undefined` to keep the current value.

| | |
| --- | --- |
| **Fires** | In the scene music selector, when the dungeon biome track is chosen. |
| **Value** | The biome music track (`DUNGEON.biomes[DUNGEON_BIOME].music`). |
| **ctx** | `undefined` |
| **Kind contract** | The callback receives the current value and returns a replacement, or `undefined` to keep the current value. |

## Usage

```gml
// audio.music_selector is a FILTER: you receive (value, ctx) and return a
// replacement, or undefined to keep the game's value.
function deep_cuts_audio_music_selector(_value, _ctx) {
    // _value is the biome music track the selector chose
    // (DUNGEON.biomes[DUNGEON_BIOME].music).
    // _ctx is undefined.
    if (_value == undefined) return undefined; // test undefined BEFORE anything else
    // return <your replacement track>;
    return undefined; // undefined = keep the game's value
}

mmapi_filter("audio.music_selector", deep_cuts_audio_music_selector);
```

## Engine Wiring

- Seam [`audio_music_selector`](../seams/audio_music_selector.md) dispatches from `gml/scripts/SceneAudioPlayer.gml`, in the selector's dungeon-room branch: it captures `DUNGEON.biomes[DUNGEON_BIOME].music`, filters it, and returns the result. This applies to standard dungeon rooms only, and only when `DUNGEON_RUNNER.blocking_music` is false.

## See Also

- [audio.play_guard](audio.play_guard.md) - Block any sound effect before it plays.
- [dungeon.floor_enter](dungeon.floor_enter.md) - Know when a dungeon floor is entered.
