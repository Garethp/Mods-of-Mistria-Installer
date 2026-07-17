# Hook: dungeon.floor_built

Know the moment a dungeon floor's room is fully built.

`dungeon.floor_built` is an **event** hook. Register a callback with `mmapi_on`. See [Hooks](../HOOKS.md) for how registration and dispatch work.

## Contract

Fires in the dungeon floor entry bracket, right after `build_dungeon_room()` and before the runner's room start (`DUNGEON_RUNNER.on_room_start()`). ctx is `{ runner, floor, biome, room_id, grid, level, gm_room, impl }`, each field filled best-effort: every read is wrapped on its own, and a failed read leaves that field absent from the struct.

The bracket captures the ctx fields once, at its top (before the build) and passes the same struct instance to all three of its events, so values a handler stashed on ctx at [dungeon.floor_enter](dungeon.floor_enter.md) are still there here. The full sequence runs: [dungeon.runner_created](dungeon.runner_created.md), then [dungeon.floor_enter](dungeon.floor_enter.md), then [dungeon.room_build_begin](dungeon.room_build_begin.md), then `build_dungeon_room()`, then `dungeon.floor_built`.

| | |
| --- | --- |
| **Fires** | In the dungeon floor entry bracket, right after `build_dungeon_room()` and before the runner's room start. |
| **ctx** | `{ runner, floor, biome, room_id, grid, level, gm_room, impl }` - each field filled best-effort. |
| **Kind contract** | The callback observes the moment. Its return value is ignored. |

### The ctx struct

Every field is filled best-effort: a failed read leaves the field absent (not `undefined`-valued), so prefer `_ctx[$ "field"]` over dot access when in doubt. All fields were captured at the top of the bracket, before `build_dungeon_room()` ran.

- `runner` - the live `DungeonRunner` (`DUNGEON_RUNNER`).
- `floor` - the current floor number, read from `DUNGEON_FLOOR`.
- `biome` - the current dungeon biome, read from `DUNGEON_BIOME`.
- `room_id` - the current room, read from `room()` at the top of the bracket.
- `grid` - the active grid, read from `GRID`. By this event the builder has filled it.
- `level` - the runner's current level entry, `runner.current_level()`.
- `gm_room` - the level's GameMaker room, `level.gm_room`.
- `impl` - the level's implementation, `level.impl`.

`level`, `gm_room`, and `impl` fill in one shared read: if `current_level()` throws, all three are absent.

## Usage

```gml
// dungeon.floor_built is an EVENT: mmapi calls you after it happens.
// You cannot change or stop it here; the return value is ignored.
function floor_decorator_dungeon_floor_built(_ctx) {
    // _ctx is { runner, floor, biome, room_id, grid, level, gm_room, impl }.
    //   .runner  - the live DungeonRunner (DUNGEON_RUNNER).
    //   .floor   - the current floor number (DUNGEON_FLOOR).
    //   .biome   - the current dungeon biome (DUNGEON_BIOME).
    //   .room_id - room() at the top of the bracket.
    //   .grid    - the active grid (GRID); build_dungeon_room() has run.
    //   .level   - runner.current_level(), this floor's itinerary entry.
    //   .gm_room - level.gm_room.
    //   .impl    - level.impl.
    // Every field is best-effort: a failed read leaves it absent, so use
    // _ctx[$ "field"] when you are not sure it exists.
    var _grid = _ctx[$ "grid"];
    if (_grid == undefined) return;
    // your code here - the room is built; the runner's room start is next
}

// inside your latched register function (see Mod Anatomy):
mmapi_on("dungeon.floor_built", floor_decorator_dungeon_floor_built);
```

## Engine Wiring

- Seam [`dungeon_floor_bracket`](../seams/dungeon_floor_bracket.md) dispatches from `gml/scripts/GameplaySystems/Dungeon/enter_dungeon.gml`, bracketing `build_dungeon_room()`. This emit lands on the line after the build, before `DUNGEON_RUNNER.on_room_start()`.

## See Also

- [dungeon.floor_enter](dungeon.floor_enter.md) - This hook fires with the same ctx, before the room builds.
- [dungeon.room_build_begin](dungeon.room_build_begin.md) - This hook fires at the last moment before `build_dungeon_room()`.
- [dungeon.runner_created](dungeon.runner_created.md) - This hook fires once as the dungeon run starts.
- [dungeon.ladder_spawn](dungeon.ladder_spawn.md) - Veto the descent ladder.
