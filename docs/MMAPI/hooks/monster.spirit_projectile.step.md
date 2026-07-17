# Hook: monster.spirit_projectile.step

Stop a spirit projectile mid-flight.

`monster.spirit_projectile.step` is a **guard** hook. Register a callback with `mmapi_guard`. See [Hooks](../HOOKS.md) for how registration and dispatch work.

## Contract

Fires each step of a spirit projectile. ctx is the projectile instance. Return `false` to veto the step and destroy the projectile. `undefined` or `true` allows.

A veto does not merely pause the projectile. The engine runs `instance_destroy(); return;`, so the projectile is removed on the spot. Because the guard fires every step of every live projectile, a standing veto condition removes each projectile the first step it matches.

| | |
| --- | --- |
| **Fires** | Each step of a live spirit projectile, in `obj_monster_spirit_projectile`'s step. |
| **ctx** | The projectile instance (`obj_monster_spirit_projectile`). |
| **Kind contract** | The callback returns `false` to veto the action. `undefined` or `true` allows it. Guards fail open: a callback that throws counts as allow. |

### The ctx parameter

- The `obj_monster_spirit_projectile` instance stepping. Read `x` and `y` for its position to veto selectively (e.g. inside a protected radius).

## Usage

```gml
// monster.spirit_projectile.step is a GUARD: return false to block it,
// undefined (or true) to allow. Guards fail OPEN - if your handler crashes,
// the action happens.
function projectile_ward_monster_spirit_projectile_step(_ctx) {
    // _ctx is the spirit projectile instance (obj_monster_spirit_projectile).
    //   .x / .y - its current position.
    // Fires every step of every live projectile.
    // if (<your condition>) {
    //     return false; // veto - the engine then runs: instance_destroy(); return;
    // }
    return undefined; // let it fly
}

// inside your latched register function (see Mod Anatomy):
mmapi_guard("monster.spirit_projectile.step", projectile_ward_monster_spirit_projectile_step);
```

## Engine Wiring

- Seam [`monster_spirit_projectile_step`](../seams/monster_spirit_projectile_step.md) dispatches from `gml/objects/Combat/obj_monster_spirit_projectile.gml`, in the projectile's step right after the flight loop sound starts. On veto the engine runs `instance_destroy(); return;`.

## See Also

- [monster.shroom.should_hide](monster.shroom.should_hide.md) - This other per-species monster guard keeps shrooms visible.
- [monster.step_begin](monster.step_begin.md) - This hook fires for every monster, every frame.
- [combat.damage](combat.damage.md) - Reshape or drop the hit if a projectile does connect.
