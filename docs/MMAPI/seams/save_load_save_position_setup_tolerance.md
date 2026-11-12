# Engine Fix: save_load_save_position_setup_tolerance

A save position in an unknown location places Ari at the home safe spot instead of crashing before the load begins.

`save_load_save_position_setup_tolerance` is an **engine fix**, an anchored edit with no hook behind it. Nothing dispatches. It is the entry half of the save position pair, with [save_load_save_position_tolerance](save_load_save_position_tolerance.md) as the load half that covers the same record inside `load_game`. See [Seams](../SEAMS.md).

## Placement

| | |
| --- | --- |
| **File** | `gml/objects/Setup.gml` |
| **Locator** | text anchor on the `save_position` read in `enter_game`'s load arm |
| **Feeds** | (no hook) |
| **Marker** | `mmapi_save_position_setup_tolerance` |

## The Edit

The replace resolves the saved position's location before it is deserialized. The record is copied into a local, with the marker on that line, and its `location_id` is resolved with `try_string_to_location_id`. When the result is `undefined` the fix logs a warn naming the location and builds a `LocationPosition` in `LocationId.PlayerHome` at the home safe spot, choosing between the small and large house vectors from the player file's `size_upgrade` the way `player_home_safe_position` chooses them. A known location takes the pristine `deserialize_location_position` call.

Ari's saved position is read twice when a save is loaded. The first read is here in `Setup.enter_game`, which deserializes it to pick the room the taxi enters, before `Game` exists and before `load_game` runs. That read is outside the load's own catch, so an unknown location here is a hard crash with a backtrace through `LocationPosition.gml`, not the return to the title screen the other load sites produce. A player who saved while standing in a room a mod added, then removed the mod, cannot load the save.

The fix cannot call `player_home_safe_position` itself, because that helper reads `DECOR`, which `Game` constructs later. The two vectors it would choose are therefore chosen here from the same `size_upgrade` test. Ari enters the game standing in her house, and the end of day writes a fresh position from her bed as always.

For names that resolve, the pristine call runs with the same record, so an intact install behaves exactly as before.

## See Also

- [save_load_save_position_tolerance](save_load_save_position_tolerance.md) - The load half of the pair.
- [save_load_npc_location_tolerance](save_load_npc_location_tolerance.md) - The same record on every NPC, resolved through the same converter.
