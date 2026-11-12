# Engine Fix: save_load_last_bed_tolerance

A last bed in an unknown location is dropped with a warn instead of aborting the load.

`save_load_last_bed_tolerance` is an **engine fix**, an anchored edit with no hook behind it. Nothing dispatches. See [Seams](../SEAMS.md).

## Placement

| | |
| --- | --- |
| **File** | `gml/scripts/GameplaySystems/Cycle/LoadGame.gml` |
| **Locator** | text anchor on the struct the `last_bed_used` closure returns |
| **Feeds** | (no hook) |
| **Marker** | `mmapi_save_last_bed_tolerance` |

## The Edit

The replace resolves the bed's location before the closure builds its record. Inside the `opt_and_then` closure that restores `ARI.last_bed_used`, the location is resolved with `try_string_to_location_id` into a local, with the marker on that line. When the result is `undefined` the closure logs a warn naming the location and returns `undefined`. Otherwise it returns the same `location_id` and `ni` struct as before, built from the resolved ordinal.

Sleeping in a bed records its location and node index as the last bed used, and the load resolves that location with the fatal converter. A player who slept in a bed placed in a room a mod added, and then removed the mod, cannot load the save.

The fix drops the record. The engine already recovers from an `undefined` record in `player_bed()`, which warns that it could not find the last used bed and searches every home grid for another, the same path it takes when the saved bed's node is gone. Ari wakes in whichever bed that search finds, or at the home safe spot as a fallback.

For names that resolve, the record is identical to the pristine one, so an intact install behaves exactly as before.

## See Also

- [save_load_save_position_tolerance](save_load_save_position_tolerance.md) - The other position record `LoadGame.gml` restores by location name.
- [save_load_song_override_tolerance](save_load_song_override_tolerance.md) - Another `LoadGame.gml` location read the family makes tolerant.
