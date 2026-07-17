# Seam: monster_step_begin

Emits once per monster per frame, right after the aggro update.

`monster_step_begin` is a **template seam** (`op = "emit"`). It feeds [monster.step_begin](../hooks/monster.step_begin.md). Mod authors never write seams. You register handlers for the hooks they dispatch. See [Seams](../SEAMS.md).

## Placement

| | |
| --- | --- |
| **File** | `gml/objects/Combat/par_monster.gml` |
| **Locator** | pristine context: after `self.aggro = self.patience.aggro();` in the begin step, before `aggro_flipped_this_frame` is computed |
| **Op** | `emit` |
| **Feeds** | [`monster.step_begin`](../hooks/monster.step_begin.md) |
| **ctx built** | `self` - the monster instance |
| **Marker** | `mmapi_monsters_run_step_begin` |

## The Edit

The generated emit lands in `par_monster`'s begin step, on the line after the aggro update (`self.aggro = self.patience.aggro();`) and before the engine computes `aggro_flipped_this_frame`. It calls `mmapi_emit("monster.step_begin", self)` in the uniform try/catch shape, so handlers see the monster with its aggro state already refreshed for the frame, before the monster acts on it.

Because `par_monster` is the parent of every monster, this one edit fires for every monster, every frame. The hook is a hot path, and handlers must be cheap. With zero handlers the emit early-outs on an empty registry.

## See Also

- [monster.step_begin](../hooks/monster.step_begin.md) - This is the hook that this seam dispatches.
- [monster_draw](monster_draw.md) - This is the same file's per-frame draw emit.
- [monster_death](monster_death.md) - This is the emit when the monster's last frame comes.
