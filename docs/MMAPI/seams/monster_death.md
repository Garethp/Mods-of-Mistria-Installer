# Seam: monster_death

Emits the moment a monster dies, one line before its instance is destroyed.

`monster_death` is a **template seam** (`op = "emit"`). It feeds [monster.death](../hooks/monster.death.md). Mod authors never write seams. You register handlers for the hooks they dispatch. See [Seams](../SEAMS.md).

## Placement

| | |
| --- | --- |
| **File** | `gml/scripts/Combat/MonsterUtils.gml` |
| **Locator** | structural target: `monster_death_poof`, at before `instance_destroy(self.owner);` (the owner-guard's last act) |
| **Op** | `emit` |
| **Feeds** | [`monster.death`](../hooks/monster.death.md) |
| **ctx built** | `{ monster: self.owner, monster_id: self.owner.monster_id, x: self.x, y: self.y }` |
| **Marker** | `mmapi_monsters_run_death_callbacks` |

## The Edit

The generated emit lands in the monster death routine in `MonsterUtils.gml`, on the line before `instance_destroy(self.owner);` (after the death is decided, before the instance is gone). It calls `mmapi_emit("monster.death", ...)` in the uniform try/catch shape with a ctx assembled field by field: `monster` is the dying instance (`self.owner`, still alive and readable at emit time), `monster_id` is its species id, and `x`/`y` are the death position. Handlers get one last look at the live instance (health, flags, position) and a stable id and coordinates that outlive it.

The anchor is structural. The locator names the `instance_destroy(self.owner);` statement, token-matched inside `monster_death_poof`, and places the emit `before` it. Token matching is immune to the surrounding body's whitespace and comment drift, and the anchored statement only needs to be unique within the function rather than the whole file. That statement is the last act of the owner-guard, which also pays out the death loot (the `Perk.GenerousInDefeat` drops/essence roll). The emit therefore runs after all death loot handling, so bonus drops a handler spawns cannot double into the engine's own payout. With zero handlers the emit early-outs on an empty registry and the routine is behaviorally identical to pristine.

## See Also

- [monster.death](../hooks/monster.death.md) - This is the hook that this seam dispatches.
- [monster_spawn](monster_spawn.md) - This is the other end of the monster's life.
- [monster_step_begin](monster_step_begin.md) - This is the per-frame observation point while the monster lives.
