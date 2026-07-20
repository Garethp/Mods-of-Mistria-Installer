# Seam: monster_draw

Emits at the end of every monster's world-space draw.

`monster_draw` is a **template seam** (`op = "emit"`). It feeds [monster.draw](../hooks/monster.draw.md). Mod authors never write seams. You register handlers for the hooks they dispatch. See [Seams](../SEAMS.md).

## Placement

| | |
| --- | --- |
| **File** | `gml/objects/Combat/par_monster.gml` |
| **Locator** | pristine context: the end of the `draw()` method, after the status-overlay block closes with `gpu_reset_extra();` |
| **Op** | `emit` |
| **Feeds** | [`monster.draw`](../hooks/monster.draw.md) |
| **ctx built** | `self` - the monster instance |
| **Marker** | `mmapi_monster_run_draw_callbacks` |

## The Edit

The generated emit lands at the end of `par_monster`'s `draw()` method, just after the block that draws the monster's status overlays (venom/frozen) resets the GPU state with `gpu_reset_extra()`. It calls `mmapi_emit("monster.draw", self)` in the uniform try/catch shape. At that point the monster sprite and its overlays are already on screen, so a handler draws on top of them, in world space, with the monster instance in hand: `x`, `y`, `z`, `health`, and everything else the instance carries. A health bar above the monster is the canonical use.

This fires once per visible monster per frame in the world-draw pass, a hot path. Keep handlers cheap. With zero handlers the emit early-outs on an empty registry.

## See Also

- [monster.draw](../hooks/monster.draw.md) - This is the hook this seam dispatches.
- [monster_step_begin](monster_step_begin.md) - This seam is the same file's per-frame logic-side emit.
