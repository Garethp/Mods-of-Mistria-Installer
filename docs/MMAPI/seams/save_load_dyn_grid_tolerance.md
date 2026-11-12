# Engine Fix: save_load_dyn_grid_tolerance

A building interior in an unknown location is left as an empty slot, and the building that owned it is removed unless animals are housed in it, instead of aborting the load.

`save_load_dyn_grid_tolerance` is an **engine fix**, an anchored edit with no hook behind it. Nothing dispatches. See [Seams](../SEAMS.md).

## Placement

| | |
| --- | --- |
| **File** | `gml/scripts/GameplaySystems/Cycle/LoadGame.gml` |
| **Locator** | text anchor on the dynamic grid loop's location resolve |
| **Feeds** | (no hook) |
| **Marker** | `mmapi_save_dyn_grid_tolerance` |

## The Edit

The replace resolves the interior's location with `try_string_to_location_id` instead of the fatal converter, with the marker on that line. When the result is `undefined` the fix logs a warn naming the slot and the location, handles the building that owned the interior, and skips the iteration with `continue`, so the slot in `DYNAMIC_GRIDS` keeps the `undefined` the loop created it with. A known location continues into the pristine trace, `initialize_grid` and `load` calls.

The owner pass walks every loaded location file and every interior file in `files`, and in each `object_list` looks for the object whose `dyn_index` equals the dropped slot. An object with no stable, or a stable where every stall is nullish, is removed from the list with a warn naming the building and the location. An object whose stable holds an animal is left in place with a warn. The test uses `is_nullish`, because an empty stall is a JSON null and the stable's own deserializer reads it the same way.

Each farm building owns a dynamic grid, its interior, saved as its own file naming the room it is an instance of. The load resolves that name through the fatal converter, so a building whose interior room a mod added aborts the load once the mod is removed.

The fix leaves the slot empty. That is the state the engine itself leaves when a building is demolished, and the saver, the end of day loops and the lost item pass already treat an empty slot as absent. The interior and everything placed in it are gone, and the next save, which the saver writes into a fresh vault, no longer carries the file. Items that were on the interior's floor are recovered by [save_load_lost_items_relocate](save_load_lost_items_relocate.md).

The building itself is treated the way a demolition would treat it. The engine refuses to demolish exactly one thing, a stable with animals in it, so an empty building is removed and its plot is cleared, whether its kind came from the mod or from the game. A building that still houses animals is left for the engine, and two outcomes follow. A vanilla kind survives, binds the emptied slot, and meets the engine's own `crash()` in the farm load, so the save returns to the title until the mod is reinstalled. Moving the animals out before the uninstall avoids that. A mod kind is dropped by the object loader's own unknown name skip, and the animals go with it, which is the engine's vanilla behavior. The pass covers every location rather than only the farm, because a mod may let buildings exist elsewhere, and it runs only when an interior is dropped.

For names that resolve, the loop runs its pristine body, so an intact install behaves exactly as before.

## See Also

- [save_load_lost_items_relocate](save_load_lost_items_relocate.md) - Recovers the floor items of the interiors this fix drops.
- [save_load_grid_file_guard](save_load_grid_file_guard.md) - The static grid loop's guard for a location the save predates.
- [save_load_blueprint_node_tolerance](save_load_blueprint_node_tolerance.md) - The object loader's own unknown name skip, which the family extends.
