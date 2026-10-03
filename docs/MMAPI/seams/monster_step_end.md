# Seam: monster_step_end

Emits once per monster per frame, after the engine's last renderable write of the end step.

`monster_step_end` is a **template seam** (`op = "emit"`). It feeds [monster.step_end](../hooks/monster.step_end.md). Mod authors never write seams. You register handlers for the hooks they dispatch. See [Seams](../SEAMS.md).

## Placement

| | |
| --- | --- |
| **File** | `gml/objects/Combat/par_monster.gml` |
| **Locator** | pristine context: after `self.status_blender.x_scale = self.image_xscale;`, the last line of the end step, before `animation_end:` |
| **Op** | `emit` |
| **Feeds** | [`monster.step_end`](../hooks/monster.step_end.md) |
| **ctx built** | `self` - the monster instance |
| **Marker** | `mmapi_monsters_run_step_end` |

## The Edit

The generated emit lands at the end of `par_monster`'s end step, after the engine has computed the frame's draw offsets from trauma and height, refreshed the shadow caster, and positioned the status overlay renderable. It calls `mmapi_emit("monster.step_end", self)` in the uniform try/catch shape. Handlers see the monster with every per-frame field the engine draws from already written, and nothing has been drawn yet.

The end step is not a draw event, so the engine's draw state is unavailable there. A draw call from a handler throws, and the dispatcher's catch logs it as a handler failure. The site suits renderables a mod owns on the instance, whose fields the engine reads when it draws.

Because `par_monster` is the parent of every monster, this one edit fires for every monster, every frame. The hook is a hot path, and handlers must be cheap. With zero handlers the emit early-outs on an empty registry.

## See Also

- [monster.step_end](../hooks/monster.step_end.md) - This is the hook that this seam dispatches.
- [monster_step_begin](monster_step_begin.md) - This is the same file's begin-step emit.
- [monster_death](monster_death.md) - This is the emit when the monster's last frame comes.
