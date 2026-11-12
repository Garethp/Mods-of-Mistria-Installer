# Engine Fix: save_load_grid_file_guard

A serializable location with no grid file in the save takes the first time setup path instead of aborting the load.

`save_load_grid_file_guard` is an **engine fix**, an anchored edit with no hook behind it. Nothing dispatches. See [Seams](../SEAMS.md).

## Placement

| | |
| --- | --- |
| **File** | `gml/scripts/GameplaySystems/Cycle/LoadGame.gml` |
| **Locator** | text anchor on the static grid loop's `serializable` test |
| **Feeds** | (no hook) |
| **Marker** | `mmapi_save_grid_file_guard` |

## The Edit

The replace adds one condition to the test that decides whether a location's grid is loaded from its file or set up for the first time. Between `location.serializable` and the `struct_names_count` call, the fix tests `grid_file != undefined`, with the marker as a trailing comment on that line. The rest of the condition and both branches are untouched.

A location that opts into serialization gets a grid file in every save written after it existed. A save written before a location mod was installed has no such file, so the loop finds `undefined` and passes it to `struct_names_count`, which throws. The engine never triggers this itself, because its own locations that became serializable are given empty files by a save migration.

The fix routes the missing file to the `else` branch, which is `first_time_grid_setup`, the same path a new game takes for the room. The room's authored priority objects and grass are stamped, Ari can enter it, and the next save writes its file. Nothing is logged, because this is the room's first visit rather than a loss. A fabricated empty file would have skipped that setup and left the room bare.

A location with a file passes the new test and takes the pristine load, so an intact install behaves exactly as before.

## See Also

- [save_load_dyn_grid_tolerance](save_load_dyn_grid_tolerance.md) - The dynamic grid loop's tolerance for an interior in an unknown location.
- [game_stats_seed_on_load](game_stats_seed_on_load.md) - The other fix that readies a save for content added after it was written.
