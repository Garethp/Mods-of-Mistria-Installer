# Hook: dungeon.floor_enter

Know the moment a dungeon floor is entered, before its room builds.

`dungeon.floor_enter` is an **event** hook. Register a callback with `mmapi_on`. See [Hooks](../HOOKS.md) for how registration and dispatch work.

## Contract

Fires in the dungeon floor entry bracket, before [dungeon.room_build_begin](dungeon.room_build_begin.md) and `build_dungeon_room()`. ctx is `{ runner, floor, biome, room_id, grid, level, gm_room, impl }`, each field filled best-effort: every read is wrapped on its own, and a failed read leaves that field absent from the struct.

The bracket builds the ctx struct once, at its top, then passes the same struct to all three of its events: `dungeon.floor_enter` then [dungeon.room_build_begin](dungeon.room_build_begin.md) then `build_dungeon_room()` then [dungeon.floor_built](dungeon.floor_built.md). Values a handler stashes on ctx here are still there when `dungeon.floor_built` fires. For a fresh run, [dungeon.runner_created](dungeon.runner_created.md) precedes the whole sequence.

| | |
| --- | --- |
| **Fires** | In the dungeon floor entry bracket, before `dungeon.room_build_begin` and `build_dungeon_room()`. |
| **ctx** | `{ runner, floor, biome, room_id, grid, level, gm_room, impl }` - each field filled best-effort. |
| **Kind contract** | The callback observes the moment. Its return value is ignored. |

### The ctx struct

Every field is filled best-effort: a failed read leaves the field absent (not `undefined`-valued), so prefer `_ctx[$ "field"]` over dot access when in doubt.

- `runner` - the live `DungeonRunner` (`DUNGEON_RUNNER`).
- `floor` - the current floor number, read from `DUNGEON_FLOOR`.
- `biome` - the current dungeon biome, read from `DUNGEON_BIOME`.
- `room_id` - the current room, read from `room()` at emit time.
- `grid` - the active grid, read from `GRID`.
- `level` - the runner's current level entry, `runner.current_level()`.
- `gm_room` - the level's GameMaker room, `level.gm_room`.
- `impl` - the level's implementation, `level.impl`.

`level`, `gm_room`, and `impl` fill in one shared read: if `current_level()` throws, all three are absent.

## Usage

```gml
// dungeon.floor_enter is an EVENT: the return value is ignored.
// You cannot change or stop it here; the return value is ignored.
function depth_gauge_dungeon_floor_enter(_ctx) {
    // _ctx is { runner, floor, biome, room_id, grid, level, gm_room, impl }.
    //   .runner  - the live DungeonRunner (DUNGEON_RUNNER).
    //   .floor   - the current floor number (DUNGEON_FLOOR).
    //   .biome   - the current dungeon biome (DUNGEON_BIOME).
    //   .room_id - room() at emit time.
    //   .grid    - the active grid (GRID); the room builder has not run yet.
    //   .level   - runner.current_level(), this floor's itinerary entry.
    //   .gm_room - level.gm_room.
    //   .impl    - level.impl.
    // Every field is best-effort: a failed read leaves it absent, so use
    // _ctx[$ "field"] when you are not sure it exists.
    var _floor = _ctx[$ "floor"];
    if (_floor == undefined) return;
    // your code here - e.g. toast the floor number as the player descends
}

// inside your latched register function (see Mod Anatomy):
mmapi_on("dungeon.floor_enter", depth_gauge_dungeon_floor_enter);
```

## Engine Wiring

- Seam [`dungeon_floor_bracket`](../seams/dungeon_floor_bracket.md) dispatches from `gml/scripts/GameplaySystems/Dungeon/enter_dungeon.gml`, bracketing `build_dungeon_room()`. This emit lands just before it, ahead of `dungeon.room_build_begin`.

## See Also

- [dungeon.runner_created](dungeon.runner_created.md) - This event fires once as the dungeon run starts.
- [dungeon.room_build_begin](dungeon.room_build_begin.md) - This event marks the last moment before `build_dungeon_room()`.
- [dungeon.floor_built](dungeon.floor_built.md) - This event fires once the floor's room is fully built.
- [audio.music_selector](audio.music_selector.md) - Swap the dungeon biome music track.
