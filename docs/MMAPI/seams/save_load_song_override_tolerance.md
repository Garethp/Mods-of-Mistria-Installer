# Engine Fix: save_load_song_override_tolerance

A song override keyed to an unknown location is skipped instead of aborting the load.

`save_load_song_override_tolerance` is an **engine fix**, an anchored edit with no hook behind it. Nothing dispatches. See [Seams](../SEAMS.md).

## Placement

| | |
| --- | --- |
| **File** | `gml/scripts/GameplaySystems/Cycle/LoadGame.gml` |
| **Locator** | text anchor on the `song_overrides` restore loop's assignment |
| **Feeds** | (no hook) |
| **Marker** | `mmapi_save_song_override_tolerance` |

## The Edit

The replace rewrites the one assignment inside the loop that walks the saved `song_overrides` keys. The key is resolved with `try_string_to_location_id` into a local, with the marker on that line. When the result is `undefined` the iteration is skipped with `continue`, and a warn names the location only when the key held an override. A resolved key assigns `overrides[keys[i]]` into `ARI.song_overrides` at the resolved ordinal, as before.

The saver writes the song override table with one key per `LocationId` member, most of them holding no override, and the load walks every key through the fatal converter. A save written while a location mod was installed therefore names every room that mod added, and removing the mod aborts the load whether or not a crystal resonator was ever placed in one of those rooms. Locations are fiddle content, so mods can mint location names.

The fix skips the unknown key. The warn is held back for an empty key, because a removed location leaves one behind in every save and dropping it loses nothing, which is how the loop already treats a vanilla key with no override. A key that held an override names itself in the log, and the player loses one song choice for a room that no longer exists.

For names that resolve, the loop assigns the same value at the same ordinal, so an intact install behaves exactly as before.

## See Also

- [save_load_props_tolerance](save_load_props_tolerance.md) - The other table the saver keys by every location.
- [save_load_last_bed_tolerance](save_load_last_bed_tolerance.md) - Another `LoadGame.gml` location read the family makes tolerant.
