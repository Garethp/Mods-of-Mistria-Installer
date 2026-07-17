# Seam: monster_death

Emits the moment a monster dies, one line before its instance is destroyed.

`monster_death` is a **template seam** (`op = "emit"`). It feeds [monster.death](../hooks/monster.death.md). Mod authors never write seams. You register handlers for the hooks they dispatch. See [Seams](../SEAMS.md).

## Placement

| | |
| --- | --- |
| **File** | `gml/scripts/Combat/MonsterUtils.gml` |
| **Locator** | pristine context: immediately before `instance_destroy(self.owner);` in the death routine |
| **Op** | `emit` |
| **Feeds** | [`monster.death`](../hooks/monster.death.md) |
| **ctx built** | `{ monster: self.owner, monster_id: self.owner.monster_id, x: self.x, y: self.y }` |
| **Marker** | `mmapi_monsters_run_death_callbacks` |

## The Edit

The generated emit lands in the monster death routine in `MonsterUtils.gml`, on the line before `instance_destroy(self.owner);` (after the death is decided, before the instance is gone). It calls `mmapi_emit("monster.death", ...)` in the uniform try/catch shape with a ctx assembled field by field: `monster` is the dying instance (`self.owner`, still alive and readable at emit time), `monster_id` is its species id, and `x`/`y` are the death position. Handlers get one last look at the live instance (health, flags, position) and a stable id and coordinates that outlive it.

The anchor sits just above the engine's void-powder drop check (`Requirement.HasVoidSight`), so the emit precedes all death loot handling. With zero handlers the emit early-outs on an empty registry and the routine is behaviorally identical to pristine.

## See Also

- [monster.death](../hooks/monster.death.md) - This is the hook that this seam dispatches.
- [monster_spawn](monster_spawn.md) - This is the other end of the monster's life.
- [monster_step_begin](monster_step_begin.md) - This is the per-frame observation point while the monster lives.
