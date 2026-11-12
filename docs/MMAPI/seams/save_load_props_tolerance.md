# Engine Fix: save_load_props_tolerance

A props entry for an unknown location is dropped with a warn instead of aborting the load.

`save_load_props_tolerance` is an **engine fix**, an anchored edit with no hook behind it. Nothing dispatches. See [Seams](../SEAMS.md).

## Placement

| | |
| --- | --- |
| **File** | `gml/scripts/GameplaySystems/Cycle/LoadGame.gml` |
| **Locator** | text anchor on the `PROPS.world` restore |
| **Feeds** | (no hook) |
| **Marker** | `mmapi_save_props_tolerance` |

## The Edit

The replace swaps the converter the `apply_struct_to_array` call passes for `files.gamedata.props`. The fatal `string_to_location_id` argument becomes a closure, with the marker on its opening line, that resolves the key with `try_string_to_location_id`, logs a warn naming the location when the result is `undefined`, and returns the result either way. The `PROPS.world` target and the `ListFromArray` value modifier are untouched.

The saver writes the props table with one key for every `LocationId` member, including locations that hold no prop, so a save written while a location mod was installed names every room that mod added. The load restores the table through the fatal converter, which means removing the mod aborts the load of every save written while it was present, not only saves made inside its rooms. Locations are fiddle content, so mods can mint location names.

The fix is the swap the `items_sold` restore beside it already makes. `apply_struct_to_array` skips a key whose converter does not return a number, so the unknown key is dropped and the closure names it in the log. A dropped entry costs at most the cutscene props the mod had placed in its own room, which no longer exists.

For names that resolve, the tolerant variant returns the same ordinal as the fatal one, so an intact install behaves exactly as before.

## See Also

- [save_load_song_override_tolerance](save_load_song_override_tolerance.md) - The other table the saver keys by every location.
- [save_load_used_objects_tolerance](save_load_used_objects_tolerance.md) - The same converter swap on the daily used-objects flags.
