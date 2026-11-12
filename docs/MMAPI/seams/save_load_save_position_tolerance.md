# Engine Fix: save_load_save_position_tolerance

A save position in an unknown location is dropped with a warn instead of aborting the load.

`save_load_save_position_tolerance` is an **engine fix**, an anchored edit with no hook behind it. Nothing dispatches. It is the load half of the save position pair, with [save_load_save_position_setup_tolerance](save_load_save_position_setup_tolerance.md) as the entry half that reads the same record first. See [Seams](../SEAMS.md).

## Placement

| | |
| --- | --- |
| **File** | `gml/scripts/GameplaySystems/Cycle/LoadGame.gml` |
| **Locator** | text anchor on the `ARI.save_position` restore |
| **Feeds** | (no hook) |
| **Marker** | `mmapi_save_position_tolerance` |

## The Edit

The replace wraps the deserializer the `opt_and_then` call passes. The bare `deserialize_location_position` argument becomes a closure, with the marker on its opening line, that resolves the record's `location_id` with `try_string_to_location_id`, logs a warn naming the location and returns `undefined` when the result is `undefined`, and otherwise returns `deserialize_location_position` of the record.

This is the second read of the saved position, inside `load_game`, through the same fatal converter the entry half guards. Without it, a save the entry half has already redirected home still aborts a few lines into the load.

The fix leaves `ARI.save_position` `undefined` for the session. That is safe because the field's only reader is the saver, and both callers of the saver assign a fresh position on the line before they save, the end of day menu from `player_wake_position` and the journal from the journal's own spot. It is also how the engine treats the last bed record, `undefined` until it is derived again.

For names that resolve, the closure returns the same `LocationPosition` as the pristine call, so an intact install behaves exactly as before.

## See Also

- [save_load_save_position_setup_tolerance](save_load_save_position_setup_tolerance.md) - The entry half of the pair.
- [save_load_last_bed_tolerance](save_load_last_bed_tolerance.md) - The other position record `LoadGame.gml` restores by location name.
