# Seam: audio_play_guard

Puts a veto check at the head of the engine's one sound-effect entry point.

`audio_play_guard` is a **template seam** (`op = "guard"`). It feeds [audio.play_guard](../hooks/audio.play_guard.md). Mod authors never write seams. You register handlers for the hooks they dispatch. See [Seams](../SEAMS.md).

## Placement

| | |
| --- | --- |
| **File** | `gml/scripts/Tango.gml` |
| **Locator** | structural target: the head of `play()` |
| **Op** | `guard` |
| **Feeds** | [`audio.play_guard`](../hooks/audio.play_guard.md) |
| **ctx built** | `{ asset_name: asset_name }` |
| **On veto** | `return undefined;` |
| **Marker** | `mmapi_audio_run_play_guards` |

## The Edit

The generated dispatch lands at the head of `TANGO.play(asset_name, ...)`, before any of the engine's play logic. It calls `mmapi_check_guards("audio.play_guard", { asset_name: asset_name })` in the uniform try/catch shape. When any guard returns `false`, the injected line runs `return undefined;`, so the sound never starts and the caller sees the same `undefined` a failed play would produce. With zero handlers the seam is behaviorally identical to pristine. The guard check early-outs on an empty registry.

`TANGO.play` is the funnel for every sound effect in the game, so this one edit makes every SFX vetoable, which is also why the hook is a hot path.

## See Also

- [audio.play_guard](../hooks/audio.play_guard.md) - This is the hook that this seam dispatches.
- [audio_music_selector](audio_music_selector.md) - This is the other audio seam.
