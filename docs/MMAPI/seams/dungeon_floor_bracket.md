# Seam: dungeon_floor_bracket

Brackets dungeon floor entry with three emits: floor enter, room-build begin, and floor built.

`dungeon_floor_bracket` is a **text seam** (`anchor` + `replace`). It feeds [dungeon.floor_enter](../hooks/dungeon.floor_enter.md), [dungeon.room_build_begin](../hooks/dungeon.room_build_begin.md), and [dungeon.floor_built](../hooks/dungeon.floor_built.md). Mod authors never write seams. You register handlers for the hooks they dispatch. See [Seams](../SEAMS.md).

## Placement

| | |
| --- | --- |
| **File** | `gml/scripts/GameplaySystems/Dungeon/enter_dungeon.gml` |
| **Locator** | text anchor: the floor-entry sequence from the `GS_MINES_FLOOR` bookkeeping through `build_dungeon_room(GRID)` to `DUNGEON_RUNNER.on_room_start()` |
| **Feeds** | [`dungeon.floor_enter`](../hooks/dungeon.floor_enter.md), [`dungeon.room_build_begin`](../hooks/dungeon.room_build_begin.md), [`dungeon.floor_built`](../hooks/dungeon.floor_built.md) |
| **ctx built** | `{ runner, floor, biome, room_id, grid, level, gm_room, impl }` - one struct, field-by-field, per-field `try`/`catch`, shared by all three emits |
| **Marker** | `mmapi_dungeon_run_floor_enter` |

## The Edit

One text seam, three hooks. The replace brackets `build_dungeon_room()` inside the floor-entry sequence:

1. `dungeon.floor_enter` emits after the `GS_MINES_FLOOR` bookkeeping, before anything is built.
2. `dungeon.room_build_begin` emits immediately after, still before `build_dungeon_room(GRID)`. This shares the same ctx and is the last observation point before the room exists.
3. `build_dungeon_room(GRID)` runs.
4. `dungeon.floor_built` emits right after the build, before `DUNGEON_RUNNER.on_room_start()`.

All three emits share one `__mmapi_dungeon_ctx`, assembled once before the first emit. It is built field-by-field, each read in its own `try`/`catch`: `runner` (`DUNGEON_RUNNER`), `floor` (`DUNGEON_FLOOR`), `biome` (`DUNGEON_BIOME`), `room_id` (`room()`), `grid` (`GRID`), then `level`, `gm_room`, and `impl` off `DUNGEON_RUNNER.current_level()`. That per-field shape is the "best-effort" the three hook docs promise: a global that is missing at floor entry leaves its field unset without killing the emits. Each emit is additionally wrapped in its own try/catch, so a handler crashing on `floor_enter` stops neither `room_build_begin` nor the build itself.

## See Also

- [dungeon.floor_enter](../hooks/dungeon.floor_enter.md) - This is the first of the three hooks this seam dispatches.
- [dungeon.room_build_begin](../hooks/dungeon.room_build_begin.md) - This is the second hook, the last moment before `build_dungeon_room()`.
- [dungeon.floor_built](../hooks/dungeon.floor_built.md) - This is the third hook, where the room exists but the runner has not started it yet.
- [dungeon_runner_created](dungeon_runner_created.md) - This seam is the same file's run-start emit, with the same ctx shape.
