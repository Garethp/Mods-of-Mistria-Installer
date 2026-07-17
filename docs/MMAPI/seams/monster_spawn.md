# Seam: monster_spawn

Intercepts `spawn_monster()` so mods can move, replace, or cancel every monster spawn.

`monster_spawn` is a **text seam** (`anchor` + `replace`). It feeds [monster.spawn](../hooks/monster.spawn.md). Mod authors never write seams. You register handlers for the hooks they dispatch. See [Seams](../SEAMS.md).

## Placement

| | |
| --- | --- |
| **File** | `gml/scripts/Combat/Monsters.gml` |
| **Locator** | text anchor: the head of `spawn_monster(xx, yy, monster_id)`, before the `MONSTER_PROTOTYPES` lookup |
| **Op** | text (`anchor` + `replace`) |
| **Feeds** | [`monster.spawn`](../hooks/monster.spawn.md) |
| **Value filtered** | `{ x: xx, y: yy, monster_id: monster_id, cancel: false }` |
| **ctx built** | `undefined` |
| **Marker** | `mmapi_monsters_run_spawn_filters` |

## The Edit

The injected block builds the value struct `{ x: xx, y: yy, monster_id: monster_id, cancel: false }` from the function's arguments and runs it through `mmapi_apply_filters("monster.spawn", ..., undefined)` inside a try/catch, so a failing dispatch leaves the pristine spawn untouched.

The result is handled defensively. If it is not `undefined`, the cancel switch is checked first. `cancel == true` makes the function `return undefined;`, suppressing the spawn entirely (callers of `spawn_monster` see the same `undefined` a failed spawn would produce). Otherwise each field is re-read in its own try/catch: `xx`, `yy`, and `monster_id` are written back one at a time, so a handler that returns a partial or malformed struct rewrites only the fields it actually carries and cannot crash the spawn. Control then falls into the pristine `var config = MONSTER_PROTOTYPES[monster_id];` with the possibly-rewritten position and species id. Moving or replacing the monster is just writing `x`, `y`, or `monster_id`.

## See Also

- [monster.spawn](../hooks/monster.spawn.md) - This is the hook that this seam dispatches.
- [monster_death](monster_death.md) - This is the other end of the monster's life.
- [fsm_transition](fsm_transition.md) - This is the other cancel-switch filter seam, with the same struct-and-`cancel` shape.
