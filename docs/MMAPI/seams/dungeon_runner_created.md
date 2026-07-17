# Seam: dungeon_runner_created

Emits the birth of a dungeon run, after `DUNGEON_RUNNER` is constructed and before the first floor loads.

`dungeon_runner_created` is a **text seam** (`anchor` + `replace`). It feeds [dungeon.runner_created](../hooks/dungeon.runner_created.md). Mod authors never write seams. You register handlers for the hooks they dispatch. See [Seams](../SEAMS.md).

## Placement

| | |
| --- | --- |
| **File** | `gml/scripts/GameplaySystems/Dungeon/enter_dungeon.gml` |
| **Locator** | text anchor: the whole `enter_dungeon()` function body |
| **Feeds** | [`dungeon.runner_created`](../hooks/dungeon.runner_created.md) |
| **ctx built** | `{ runner, floor, biome, room_id, grid, level, gm_room, impl }` - field-by-field, per-field `try`/`catch` |
| **Marker** | `mmapi_dungeon_run_runner_created` |

## The Edit

The replace rewrites `enter_dungeon()`. After `DUNGEON_RUNNER = new DungeonRunner(itinerary, start_floor)` and before the `goto_gm_room(...)` that jumps to the first level's room, it assembles `__mmapi_dungeon_ctx` and emits `dungeon.runner_created`.

The ctx is built field-by-field, each read in its own `try`/`catch`: `runner` (the fresh `DUNGEON_RUNNER`), `floor` (`DUNGEON_FLOOR`), `biome` (`DUNGEON_BIOME`), `room_id` (`room()`, read before the `goto`, so still the room the player is entering from), `grid` (`GRID`), then `level`, `gm_room`, and `impl` off `DUNGEON_RUNNER.current_level()`. That per-field shape is the "best-effort" the hook doc promises: a global that is missing or not yet meaningful at this moment leaves its field unset instead of killing the emit. The emit itself sits in its own try/catch, so a throwing handler cannot stop the run from starting.

## See Also

- [dungeon.runner_created](../hooks/dungeon.runner_created.md) - This is the hook this seam dispatches.
- [dungeon_floor_bracket](dungeon_floor_bracket.md) - This is the same file's per-floor bracket. It fires on every floor of the run this seam starts.
- [dungeon_ladder_spawn](dungeon_ladder_spawn.md) - This seam guards the exit ladder inside the run.
