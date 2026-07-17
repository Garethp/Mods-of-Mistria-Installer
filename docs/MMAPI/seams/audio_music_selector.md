# Seam: audio_music_selector

Puts a filter on the dungeon biome music track as the scene selector picks it.

`audio_music_selector` is a **text seam**, a verbatim `anchor`/`replace` edit. It feeds [audio.music_selector](../hooks/audio.music_selector.md). Mod authors never write seams. You register handlers for the hooks they dispatch. See [Seams](../SEAMS.md).

## Placement

| | |
| --- | --- |
| **File** | `gml/scripts/SceneAudioPlayer.gml` |
| **Locator** | text anchor on the dungeon-room branch of the scene music selector |
| **Feeds** | [`audio.music_selector`](../hooks/audio.music_selector.md) |
| **Value filtered** | the biome music track, `DUNGEON.biomes[DUNGEON_BIOME].music` |
| **ctx built** | `undefined` |
| **Marker** | `mmapi_audio_run_music_selector_filters` |

## The Edit

The pristine selector handles standard dungeon rooms (`is_dungeon_room(room())` and not `is_special_dungeon_room(room())`) with two returns: `undefined` while `DUNGEON_RUNNER.blocking_music` is set, else `DUNGEON.biomes[DUNGEON_BIOME].music`. The replace touches only the non-blocking branch: the biome track is captured into `__mmapi_dungeon_music`, run through `mmapi_apply_filters("audio.music_selector", __mmapi_dungeon_music, undefined)` in a try/catch, and returned. The blocking branch still returns `undefined` untouched, so the hook fires only when the dungeon biome track is actually chosen.

With zero handlers the filter hands the track back unchanged and the branch returns exactly what pristine returned.

## See Also

- [audio.music_selector](../hooks/audio.music_selector.md) - This is the hook that this seam dispatches.
- [audio_play_guard](audio_play_guard.md) - This is the other audio seam, which is a veto check on every sound effect.
