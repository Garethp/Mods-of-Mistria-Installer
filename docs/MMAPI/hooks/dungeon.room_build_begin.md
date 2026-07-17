# Hook: dungeon.room_build_begin

Know the last moment before a dungeon room is built.

`dungeon.room_build_begin` is an **event** hook. Register a callback with `mmapi_on`. See [Hooks](../HOOKS.md) for how registration and dispatch work.

## Contract

Fires immediately before `build_dungeon_room()`, right after [dungeon.floor_enter](dungeon.floor_enter.md) with the same ctx. ctx is `{ runner, floor, biome, room_id, grid, level, gm_room, impl }`, each field filled best-effort: every read is wrapped on its own, and a failed read leaves that field absent from the struct.

No engine work runs between `dungeon.floor_enter` and this event. The bracket dispatches them back to back, and both receive the same ctx struct instance the bracket built at its top, so values a `floor_enter` handler stashed on ctx are readable here. The full sequence runs: [dungeon.runner_created](dungeon.runner_created.md), then [dungeon.floor_enter](dungeon.floor_enter.md), then `dungeon.room_build_begin`, then `build_dungeon_room()`, then [dungeon.floor_built](dungeon.floor_built.md).

| | |
| --- | --- |
| **Fires** | Immediately before `build_dungeon_room()`, right after `dungeon.floor_enter`. |
| **ctx** | `{ runner, floor, biome, room_id, grid, level, gm_room, impl }` - each field filled best-effort. |
| **Kind contract** | The callback observes the moment. Its return value is ignored. |

### The ctx struct

Every field is filled best-effort: a failed read leaves the field absent (not `undefined`-valued), so prefer `_ctx[$ "field"]` over dot access when in doubt.

- `runner` - the live `DungeonRunner` (`DUNGEON_RUNNER`).
- `floor` - the current floor number, read from `DUNGEON_FLOOR`.
- `biome` - the current dungeon biome, read from `DUNGEON_BIOME`.
- `room_id` - the current room, read from `room()` at the top of the bracket.
- `grid` - the active grid, read from `GRID`, which the room builder has not populated yet.
- `level` - the runner's current level entry, `runner.current_level()`.
- `gm_room` - the level's GameMaker room, `level.gm_room`.
- `impl` - the level's implementation, `level.impl`.

`level`, `gm_room`, and `impl` fill in one shared read: if `current_level()` throws, all three are absent.

## Usage

```gml
// dungeon.room_build_begin is an EVENT: mmapi calls you after it happens.
// You cannot change or stop it here; the return value is ignored.
function room_planner_dungeon_room_build_begin(_ctx) {
    // _ctx is { runner, floor, biome, room_id, grid, level, gm_room, impl }.
    //   .runner  - the live DungeonRunner (DUNGEON_RUNNER).
    //   .floor   - the current floor number (DUNGEON_FLOOR).
    //   .biome   - the current dungeon biome (DUNGEON_BIOME).
    //   .room_id - room() at the top of the bracket.
    //   .grid    - the active grid (GRID), not yet populated by the builder.
    //   .level   - runner.current_level(), this floor's itinerary entry.
    //   .gm_room - level.gm_room.
    //   .impl    - level.impl.
    // Every field is best-effort: a failed read leaves it absent, so use
    // _ctx[$ "field"] when you are not sure it exists.
    var _grid = _ctx[$ "grid"];
    if (_grid == undefined) return;
    // your code here - this is the last moment before build_dungeon_room()
}

// inside your latched register function (see Mod Anatomy):
mmapi_on("dungeon.room_build_begin", room_planner_dungeon_room_build_begin);
```

## Engine Wiring

- Seam [`dungeon_floor_bracket`](../seams/dungeon_floor_bracket.md) dispatches from `gml/scripts/GameplaySystems/Dungeon/enter_dungeon.gml`, bracketing `build_dungeon_room()`. This emit is the second of the bracket's three, on the line before the build.

## See Also

- [dungeon.floor_enter](dungeon.floor_enter.md) - This hook fires one event earlier, with the same ctx.
- [dungeon.floor_built](dungeon.floor_built.md) - The floor's room is fully built.
- [dungeon.runner_created](dungeon.runner_created.md) - This event fires once as the dungeon run starts.
