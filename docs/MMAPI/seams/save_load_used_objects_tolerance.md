# Engine Fix: save_load_used_objects_tolerance

Lets a save whose daily used-objects flags name a since-removed object load anyway. Unknown names are skipped, and the engine names them in the log.

`save_load_used_objects_tolerance` is an **engine fix**, an anchored edit with no hook behind it. Nothing dispatches. See [Seams](../SEAMS.md).

## Placement

| | |
| --- | --- |
| **File** | `gml/scripts/GameplaySystems/Cycle/LoadGame.gml` |
| **Locator** | text anchor on the `used_object_today` deserialize |
| **Feeds** | (no hook) |
| **Marker** | `mmapi_save_used_objects_tolerance` |

## The Edit

The replace swaps the converter the array loader calls for its tolerant variant. The ternary that restores `ARI.used_object_today` keeps its `undefined` check and its `array_bool(ObjectId.LEN)` fallback, and the `deserialize_array_bool` branch's converter becomes a closure returning `try_string_to_object_id` of its argument, with the marker as a trailing comment on that line.

The daily used-objects flags are stored as a list of object names and resolved with fatal `string_to_object_id`, while the perks, items, recipes, and spells lines around it use the tolerant `try_` variants. Object prototypes are fiddle content, so mods can mint object names, and one stale name aborts the load natively.

The fix is the same swap those lines use. Unknown names route through the skip `deserialize_array_bool` already has, which names them in the log. A skipped flag means the object counts as unused today, which is the mildest possible loss.

For names that resolve, the `try_` variant returns the same ordinal as the fatal one, so an intact install behaves exactly as before.

## See Also

- [save_load_renown_list_tolerance](save_load_renown_list_tolerance.md) - Another `LoadGame.gml` line the family makes tolerant.
- [game_stats_seed_on_load](game_stats_seed_on_load.md) - The other `LoadGame.gml` fix, which seeds the stats structs on every load.
