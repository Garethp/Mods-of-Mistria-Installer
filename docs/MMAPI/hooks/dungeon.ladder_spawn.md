# Hook: dungeon.ladder_spawn

Block the descent ladder before it spawns.

`dungeon.ladder_spawn` is a **guard** hook. Register a callback with `mmapi_guard`. See [Hooks](../HOOKS.md) for how registration and dispatch work.

## Contract

Fires at the top of `DungeonRunner.spawn_ladder()`. ctx is `{ runner, x, y }` in tile coordinates. Return `false` to veto the ladder spawn (`spawn_ladder` returns with nothing spawned). `undefined` or `true` allows.

This stops the ladder from existing at all. To let the ladder spawn but stop the player from using it, guard [interact.ladder_down_action](interact.ladder_down_action.md) instead.

| | |
| --- | --- |
| **Fires** | At the top of `DungeonRunner.spawn_ladder()`. |
| **ctx** | `{ runner, x, y }` |
| **Kind contract** | The callback returns `false` to veto the action. `undefined` or `true` allows it. Guards fail open: a callback that throws counts as allow. |

### The ctx struct

- `runner` - the `DungeonRunner` spawning the ladder.
- `x`, `y` - the ladder's spawn position, in tile coordinates.

## Usage

```gml
// dungeon.ladder_spawn is a GUARD: return false to block it, undefined (or
// true) to allow. Guards fail OPEN - if your handler crashes, the action happens.
function no_easy_exit_dungeon_ladder_spawn(_ctx) {
    // _ctx is { runner, x, y }.
    //   .runner - the DungeonRunner spawning the ladder.
    //   .x, .y  - the spawn position, in tile coordinates.
    // if (<your condition>) {
    //     return false; // veto - spawn_ladder returns, no ladder appears
    // }
    return undefined; // allow the spawn
}

// inside your latched register function (see Mod Anatomy):
mmapi_guard("dungeon.ladder_spawn", no_easy_exit_dungeon_ladder_spawn);
```

## Engine Wiring

- Seam [`dungeon_ladder_spawn`](../seams/dungeon_ladder_spawn.md) dispatches from `gml/scripts/GameplaySystems/Dungeon/DungeonRunner.gml`, at the head of `spawn_ladder()`. On veto the engine runs `return;`.

## See Also

- [interact.ladder_down_action](interact.ladder_down_action.md) - Veto the descent press on a spawned ladder.
- [dungeon.floor_built](dungeon.floor_built.md) - This event fires once the floor's room is fully built.
- [dungeon.side_room_chance](dungeon.side_room_chance.md) - Adjust side-room odds.
