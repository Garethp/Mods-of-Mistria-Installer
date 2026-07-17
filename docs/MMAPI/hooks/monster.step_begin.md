# Hook: monster.step_begin

React to every monster, every frame, right after its aggro update.

`monster.step_begin` is an **event** hook. Register a callback with `mmapi_on`. See [Hooks](../HOOKS.md) for how registration and dispatch work.

## Contract

Fires every frame for every monster in `par_monster`'s begin step, right after the aggro update. ctx is the monster instance.

The emit sits immediately after `self.aggro = self.patience.aggro();`, so `ctx.aggro` is already current for the frame when your handler runs.

| | |
| --- | --- |
| **Fires** | Every frame, in `par_monster`'s begin step, right after `self.aggro` is refreshed from the patience meter. |
| **ctx** | The monster instance. |
| **Kind contract** | The callback observes the moment. Its return value is ignored. |

### The ctx parameter

- The monster instance (a `par_monster` child). Its `aggro` field was refreshed on the line before the emit, so it reflects this frame's state.

> [!IMPORTANT]
> Hot path. This event fires for every monster, every frame. Make the callback's first check its cheapest early-exit.

## Usage

```gml
// monster.step_begin is an EVENT: mmapi calls you after it happens.
// You cannot change or stop it here; the return value is ignored.
function hive_mind_monster_step_begin(_ctx) {
    // _ctx is the monster instance (a par_monster child).
    //   .aggro - just refreshed from the patience meter; current this frame.
    //   .x / .y - world position.
    // HOT PATH: every monster, every frame. Make your first check the
    // cheapest one and get out early.
    if (!_ctx.aggro) return;
    // your per-aggro-monster logic here
}

// inside your latched register function (see Mod Anatomy):
mmapi_on("monster.step_begin", hive_mind_monster_step_begin);
```

## Engine Wiring

- Seam [`monster_step_begin`](../seams/monster_step_begin.md) dispatches from `gml/objects/Combat/par_monster.gml`, in the begin step immediately after `self.aggro = self.patience.aggro();`.

## See Also

- [monster.draw](monster.draw.md) - This hook is the per-frame draw counterpart, for world-space visuals.
- [fsm.transition](fsm.transition.md) - This hook fires on transition edges instead of every tick, so you can redirect or cancel state changes.
- [monster.spawn](monster.spawn.md) - Move, replace, or cancel the spawn before the stepping starts.
- [monster.death](monster.death.md) - This event fires the moment it all stops.
