# Hook: monster.step_end

React to every monster, every frame, after the engine has settled its frame state.

`monster.step_end` is an **event** hook. Register a callback with `mmapi_on`. See [Hooks](../HOOKS.md) for how registration and dispatch work.

## Contract

Fires every frame for every monster at the end of `par_monster`'s end step, after the engine has written the frame's draw offsets, facing, shadow caster and status overlay renderable. ctx is the monster instance.

This is not a draw pass. The engine draws the monster's sprite and the renderables it owns after the step, so a draw call from this handler throws. The moment exists for the engine's own way of attaching a visual to a monster. A renderable created on the instance with `create_renderable` is drawn by the engine, interpolated between steps, at the position and depth its fields hold. Writing those fields here, after the engine has finished its own writes for the frame, keeps a mod's renderable in step with the monster's sprite. See [Renderables](../RENDERABLES.md) for the fields and their lifetime.

| | |
| --- | --- |
| **Fires** | Every frame, at the end of `par_monster`'s end step, after the status overlay renderable is positioned. |
| **ctx** | The monster instance. |
| **Kind contract** | The callback observes the moment. Its return value is ignored. |

### The ctx parameter

- The monster instance (a `par_monster` child). Its `draw_x_offset`, `draw_y_offset`, `image_xscale` and `depth` were written earlier in the same end step, so they reflect this frame.

> [!IMPORTANT]
> Hot path. This event fires for every monster, every frame. Make the callback's first check its cheapest early-exit.

## Usage

```gml
// monster.step_end is an EVENT: the return value is ignored.
// You cannot change or stop it here; the return value is ignored.
function aura_painter_monster_step_end(_ctx) {
    // _ctx is the monster instance (a par_monster child), with this frame's
    // draw offsets, facing and depth already written by the engine.
    // This is not a draw pass, and drawing here throws. Attach visuals the
    // way the engine does: create a renderable on the monster once, keep it
    // on the instance, and write its position and depth here every frame.
    // HOT PATH: every monster, every frame. Make your first check the
    // cheapest one and get out early.
}

// inside your latched register function (see Mod Anatomy):
mmapi_on("monster.step_end", aura_painter_monster_step_end);
```

## Interactions

- [monster.step_begin](monster.step_begin.md) fires earlier in the same frame, before the monster has moved, so a renderable positioned there trails the sprite by one step.
- [monster.death](monster.death.md) fires before the instance is destroyed. A renderable's `free()` there ends the visual with the monster.

## Engine Wiring

- Seam [`monster_step_end`](../seams/monster_step_end.md) dispatches from `gml/objects/Combat/par_monster.gml`, at the end of the end step, after the status overlay renderable's last field write.

## See Also

- [monster.step_begin](monster.step_begin.md) - This hook is the per-frame logic counterpart, before the monster acts.
- [ui.draw_gui](ui.draw_gui.md) - Draw in GUI space, in a real draw pass.
- [monster.spawn](monster.spawn.md) - Move, replace, or cancel a monster before it exists.
