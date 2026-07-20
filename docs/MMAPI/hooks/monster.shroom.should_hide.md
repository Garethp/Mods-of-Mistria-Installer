# Hook: monster.shroom.should_hide

Stop shroom monsters from hiding.

`monster.shroom.should_hide` is a **guard** hook. Register a callback with `mmapi_guard`. See [Hooks](../HOOKS.md) for how registration and dispatch work.

## Contract

Fires at the top of the shroom monster's `should_hide()` check. ctx is the shroom instance. Return `false` to veto hiding (`should_hide` returns `false`). Every return other than Boolean `false` falls through to the normal distance check.

Note the polarity: this guard vetoes the *hide*, not the shroom. A veto forces `should_hide()` to answer `false`, so the shroom stays visible. It cannot force a shroom to hide.

| | |
| --- | --- |
| **Fires** | At the top of the shroom monster's `should_hide()` check. |
| **ctx** | The shroom instance (`obj_monster_shroom`). |
| **Kind contract** | Only the Boolean value `false` vetoes. Every other return allows. Guards fail open: a callback that throws counts as allow. |

### The ctx parameter

- The `obj_monster_shroom` instance asking whether to hide. Read its position or state to veto selectively.

## Usage

```gml
// monster.shroom.should_hide is a GUARD: return false to block it, undefined
// (or true) to allow. Guards fail OPEN - if your handler crashes, the action
// happens.
function shroom_spotter_monster_shroom_should_hide(_ctx) {
    // _ctx is the shroom monster instance (obj_monster_shroom).
    // A veto means "do not hide": should_hide() returns false and the shroom
    // stays visible. undefined/true falls through to the engine's normal
    // distance check.
    return false; // shrooms never hide from us
}

// inside your latched register function (see Mod Anatomy):
mmapi_guard("monster.shroom.should_hide", shroom_spotter_monster_shroom_should_hide);
```

## Engine Wiring

- Seam [`monster_shroom_should_hide`](../seams/monster_shroom_should_hide.md) dispatches from `gml/objects/Combat/obj_monster_shroom.gml`, structural target at the head of `should_hide()`. On veto the engine runs `return false;`.

## See Also

- [monster.step_begin](monster.step_begin.md) - This hook fires for every monster, every frame, right after the aggro update.
- [monster.spirit_projectile.step](monster.spirit_projectile.step.md) - This hook is the other per-species monster guard, which lets you destroy spirit projectiles mid-flight.
- [monster.spawn](monster.spawn.md) - Move, replace, or cancel a monster spawn.
