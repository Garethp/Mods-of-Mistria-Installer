# Engine Fix: save_load_lost_items_relocate

Lost items left in an unknown location or a dropped building interior are moved to Town beside the Lost and Found box instead of being forgotten.

`save_load_lost_items_relocate` is an **engine fix**, an anchored edit with no hook behind it. Nothing dispatches. See [Seams](../SEAMS.md).

## Placement

| | |
| --- | --- |
| **File** | `gml/scripts/GameplaySystems/Cycle/LoadGame.gml` |
| **Locator** | text anchor on the dynamic lost items loop |
| **Feeds** | (no hook) |
| **Marker** | `mmapi_save_lost_items_relocate` |

## The Edit

The replace inserts a relocation pass ahead of the engine's loop over `dynamic_lost_items`, which it re-emits unchanged. The pass gathers two kinds of orphaned list, with the marker on its first line. A key of the saved lost items table that is not `dynamic_lost_items` and does not resolve through `try_string_to_location_id` is one, and a `dynamic_lost_items` entry whose slot in `DYNAMIC_GRIDS` is `undefined` is the other. Each is named in a warn. The gathered lists are deserialized with the engine's own `deserialize_lost_items`, and every entry is pushed onto the Town grid's `lost_items` with its position rewritten to a four wide spread one tile apart from cell 121, 262.

Lost items are floor items the save keeps per location. The load reads only the lists of locations it knows, so a list under a room a removed mod added is never read and the next save forgets it. The same happens to the list of a building interior the [dyn grid fix](save_load_dyn_grid_tolerance.md) dropped. Those items can be quest rewards that spawned on the floor because the inventory was full, and a quest without a failsafe can stall on their loss.

The fix follows the engine's own behavior for stranded floor items, which is a lost items list on a vanilla grid. The MagicKey softlock repair a few lines above pushes into the home's list, and a demolished building's list is restored on the farm. Town is the target because the Lost and Found box lives there. Its collision region covers cells 119 to 122 by 258 to 259, so the items land just below it. They restore as floor items when Ari next enters Town, and the Monday sweep already collects Town's lost items into the box once it is unlocked. The spread is deterministic so the load consumes no random numbers.

The dynamic test needs no signal from the dyn grid fix. The saver writes a `dynamic_lost_items` entry only for an interior it saved, so a list beside an empty slot can only come from an interior dropped on this load. Nothing is written to the save, and on an intact install every key resolves and every list sits beside a loaded interior, so the pass gathers nothing and the engine's loop runs as before.

## See Also

- [save_load_dyn_grid_tolerance](save_load_dyn_grid_tolerance.md) - The fix whose dropped interiors this one recovers the floor items of.
- [save_load_props_tolerance](save_load_props_tolerance.md) - Another location keyed table the family makes tolerant.
