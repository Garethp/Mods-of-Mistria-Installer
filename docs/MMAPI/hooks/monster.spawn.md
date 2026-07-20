# Hook: monster.spawn

Change, move, or cancel any monster spawn.

`monster.spawn` is a **filter** hook. Register a callback with `mmapi_filter`. See [Hooks](../HOOKS.md) for how registration and dispatch work.

## Contract

Fires at the top of `spawn_monster()`. The filtered value is the struct `{ x, y, monster_id, cancel: false }`. ctx is `undefined`. Return the replacement struct: a handler can set `cancel` to `true` to suppress the spawn (`spawn_monster` returns `undefined`) or change `x`, `y`, and `monster_id` to move or replace the spawn. The seam tolerates an `undefined` return and re-reads fields defensively, so mutating the struct in place and returning `undefined` works too.

| | |
| --- | --- |
| **Fires** | At the top of `spawn_monster(xx, yy, monster_id)`, before the `MONSTER_PROTOTYPES` lookup. |
| **Value** | The struct `{ x, y, monster_id, cancel }`. |
| **ctx** | `undefined`. |
| **Kind contract** | The callback receives the current value and returns a replacement, or `undefined` to keep the current value. |

### The value struct

- `x`, `y` - the spawn position. Change them to move the spawn.
- `monster_id` - the species to spawn (indexes `MONSTER_PROTOTYPES`). Change it to replace the spawn.
- `cancel` - starts `false`. Set `true` to suppress the spawn: `spawn_monster` returns `undefined` and nothing spawns.

> [!IMPORTANT]
> To cancel, set `_value.cancel = true` on the value struct and return it. Returning `false` does not cancel. A filter's return replaces the value.

## Usage

```gml
// monster.spawn is a FILTER whose value struct carries a 'cancel' switch:
// set _value.cancel = true to stop it. Never return false - a filter's
// return REPLACES the value, it does not veto.
function spawn_warden_monster_spawn(_value, _ctx) {
    // _value is { x, y, monster_id, cancel }.
    //   .x / .y     - the spawn position; change them to move the spawn.
    //   .monster_id - the species (indexes MONSTER_PROTOTYPES); change it to
    //                 replace the spawn.
    //   .cancel     - starts false; set true and spawn_monster returns
    //                 undefined - nothing spawns.
    // _ctx is undefined.
    // if (<your condition>) {
    //     _value.cancel = true; // suppress this spawn
    //     return _value;
    // }
    // _value.monster_id = <replacement species>; return _value;
    return undefined; // keep the spawn as rolled
}

// inside your latched register function (see Mod Anatomy):
mmapi_filter("monster.spawn", spawn_warden_monster_spawn);
```

## Engine Wiring

- Seam [`monster_spawn`](../seams/monster_spawn.md) dispatches from `gml/scripts/Combat/Monsters.gml`, at the head of `spawn_monster()`. On cancel it runs `return undefined;`. Otherwise it re-reads `x`, `y`, and `monster_id` from the struct (each behind its own defensive `try`) before the `MONSTER_PROTOTYPES` lookup.

## See Also

- [monster.death](monster.death.md) - This hook is the other end of a monster's life.
- [fsm.transition](fsm.transition.md) - This filter is the other cancel-switch filter. It redirects or cancels state transitions.
- [monster.step_begin](monster.step_begin.md) - This hook fires for every monster, every frame, once it exists.
