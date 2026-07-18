# Hook: monster.death

Know the moment a monster dies.

`monster.death` is an **event** hook. Register a callback with `mmapi_on`. See [Hooks](../HOOKS.md) for how registration and dispatch work.

## Contract

Fires when a monster dies, just before its instance is destroyed. ctx is `{ monster, monster_id, x, y }`.

| | |
| --- | --- |
| **Fires** | In the monster death logic, immediately before `instance_destroy` removes the monster's instance. |
| **ctx** | `{ monster, monster_id, x, y }` |
| **Kind contract** | The callback observes the moment. Its return value is ignored. |

### The ctx struct

- `monster` - the dying monster instance. It is still live during your callback and destroyed immediately after.
- `monster_id` - the monster's species id.
- `x`, `y` - the death position.

## Usage

```gml
// monster.death is an EVENT: the return value is ignored.
// You cannot change or stop it here; the return value is ignored.
function trophy_hunter_monster_death(_ctx) {
    // _ctx is { monster, monster_id, x, y }.
    //   .monster    - the dying instance; destroyed right after this returns.
    //   .monster_id - the species id.
    //   .x / .y     - where it died.
    // e.g. tally the kill by species, or drop bonus loot at (_ctx.x, _ctx.y).
}

// inside your latched register function (see Mod Anatomy):
mmapi_on("monster.death", trophy_hunter_monster_death);
```

## Engine Wiring

- Seam [`monster_death`](../seams/monster_death.md) dispatches from `gml/scripts/Combat/MonsterUtils.gml`, immediately before `instance_destroy(self.owner)` destroys the monster (and before the void-powder drop roll).

## See Also

- [monster.spawn](monster.spawn.md) - This hook is the other end of a monster's life, where you move, replace, or cancel the spawn.
- [combat.damage_resolved](combat.damage_resolved.md) - This hook fires for the hit that killed it, at resolution time.
- [monster.step_begin](monster.step_begin.md) - This hook fires for every monster, every frame, while it lives.
