# Hook: dungeon.runner_created

Know the moment a dungeon run begins.

`dungeon.runner_created` is an **event** hook. Register a callback with `mmapi_on`. See [Hooks](../HOOKS.md) for how registration and dispatch work.

## Contract

Fires in `enter_dungeon()` after the run's `DungeonRunner` is constructed, before the goto to the first level's room. ctx is `{ runner, floor, biome, room_id, grid, level, gm_room, impl }`, and each field is filled best-effort. Every read is wrapped on its own, and a failed read leaves that field absent from the struct.

At emit time the runner is **not yet installed** as `DUNGEON_RUNNER`. The engine hands it to the queued room transition, and the global is assigned as that transition departs. Read the runner from ctx, not from the global.

Because the emit runs before `goto_gm_room()`, the globals in ctx (`room_id`, `grid`, `floor`, `biome`) describe the game as it stands at creation time. The first level's room has not loaded yet. `gm_room` names where the run is headed.

This is the first event of the dungeon sequence, firing once per `enter_dungeon()` call: `dungeon.runner_created`, then [dungeon.floor_enter](dungeon.floor_enter.md), then [dungeon.room_build_begin](dungeon.room_build_begin.md), then `build_dungeon_room()`, then [dungeon.floor_built](dungeon.floor_built.md).

| | |
| --- | --- |
| **Fires** | In `enter_dungeon()`, after the run's `DungeonRunner` is constructed and before the goto to the first level's room. |
| **ctx** | `{ runner, floor, biome, room_id, grid, level, gm_room, impl }` - each field filled best-effort. |
| **Kind contract** | The callback observes the moment. Its return value is ignored. |

### The ctx struct

Every field is filled best-effort: a failed read leaves the field absent (not `undefined`-valued), so prefer `_ctx[$ "field"]` over dot access when in doubt.

- `runner` - the freshly constructed `DungeonRunner`. Not yet `DUNGEON_RUNNER`. The global is installed as the queued transition departs.
- `floor` - the run's starting floor number, the runner's `current_floor`.
- `biome` - the first level's dungeon biome, `runner.current_level().biome`.
- `room_id` - the current room, read from `room()` at emit time, still the pre-dungeon room.
- `grid` - the active grid, read from `GRID`.
- `level` - the runner's current level entry, `runner.current_level()`.
- `gm_room` - the level's GameMaker room, `level.gm_room`, the room the run is about to goto.
- `impl` - the level's implementation, `level.impl`.

`level`, `gm_room`, and `impl` fill in one shared read. If `current_level()` throws, all three are absent.

## Usage

```gml
// dungeon.runner_created is an EVENT: the return value is ignored.
// You cannot change or stop it here; the return value is ignored.
function run_tracker_dungeon_runner_created(_ctx) {
    // _ctx is { runner, floor, biome, room_id, grid, level, gm_room, impl }.
    //   .runner  - the freshly built DungeonRunner (not yet DUNGEON_RUNNER;
    //              the global is installed as the queued transition departs).
    //   .floor   - the run's starting floor number (runner.current_floor).
    //   .biome   - the first level's dungeon biome (runner.current_level().biome).
    //   .room_id - room() at emit time; the first floor has not loaded yet.
    //   .grid    - the active grid (GRID).
    //   .level   - runner.current_level(), the itinerary entry for floor one.
    //   .gm_room - level.gm_room, the room the run is about to goto.
    //   .impl    - level.impl.
    // Every field is best-effort: a failed read leaves it absent, so use
    // _ctx[$ "field"] when you are not sure it exists.
    var _runner = _ctx[$ "runner"];
    if (_runner == undefined) return;
    // your code here - e.g. record where this run started
}

// inside your latched register function (see Mod Anatomy):
mmapi_on("dungeon.runner_created", run_tracker_dungeon_runner_created);
```

## Engine Wiring

- Seam [`dungeon_runner_created`](../seams/dungeon_runner_created.md) dispatches from `gml/scripts/GameplaySystems/Dungeon/enter_dungeon.gml`, between the `DungeonRunner` construction and the `goto_gm_room()` to the first level.

## See Also

- [dungeon.floor_enter](dungeon.floor_enter.md) - This hook fires as each floor is entered, before its room builds.
- [dungeon.room_build_begin](dungeon.room_build_begin.md) - This hook is the last moment before `build_dungeon_room()`.
- [dungeon.floor_built](dungeon.floor_built.md) - The floor's room is fully built.
