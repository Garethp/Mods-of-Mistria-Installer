# Engine Fix: save_load_npc_location_tolerance

An NPC saved in an unknown location is flagged with a warn instead of aborting the load.

`save_load_npc_location_tolerance` is an **engine fix**, an anchored edit with no hook behind it. Nothing dispatches. It is the location half of the NPC location pair, with [save_load_npc_location_fallback](save_load_npc_location_fallback.md) as the fallback half that relocates the NPC this edit flags. See [Seams](../SEAMS.md).

## Placement

| | |
| --- | --- |
| **File** | `gml/scripts/GameplaySystems/NPCs/Npc.gml` |
| **Locator** | text anchor on the `location_position` restore in `deserialize` |
| **Feeds** | (no hook) |
| **Marker** | `mmapi_save_npc_location_tolerance` |

## The Edit

The replace resolves the NPC's saved location before it is deserialized. A local flag, with the marker on its line, records whether `try_string_to_location_id` of the record's `location_id` returned `undefined`. When it did, the fix logs a warn naming the NPC and the location and leaves `location_position` for the fallback half to set. Otherwise the pristine `deserialize_location_position` call runs.

Every NPC blob in the save carries a location position, and `Npc.deserialize` resolves it through the fatal converter. An NPC that was standing in a room a mod added when the save was written aborts the load once the mod is removed, including a vanilla NPC a mod schedule had sent there.

The flag alone fixes nothing. The fallback half reads it a few lines later, where the engine's own recovery for a missing schedule lives, and sends the NPC to Aldaria on the basement schedule.

For names that resolve, the pristine call runs with the same record, so an intact install behaves exactly as before.

## See Also

- [save_load_npc_location_fallback](save_load_npc_location_fallback.md) - The fallback half of the pair.
- [save_load_save_position_setup_tolerance](save_load_save_position_setup_tolerance.md) - The same record on Ari, resolved before the load begins.
