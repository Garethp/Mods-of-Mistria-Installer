# Hook: monster.draw

React to every monster's draw with your own world-space visuals.

`monster.draw` is an **event** hook. Register a callback with `mmapi_on`. See [Hooks](../HOOKS.md) for how registration and dispatch work.

## Contract

Fires at the end of `par_monster`'s `draw()` method, after the monster sprite and its status overlays (venom/frozen) are drawn, once per visible monster per frame in the world-draw pass. ctx is the monster instance. This hook is observation only.

Because the emit sits after the sprite and status-overlay pass, anything you draw from the handler lands on top of the monster, in world coordinates rather than GUI coordinates.

| | |
| --- | --- |
| **Fires** | At the end of `par_monster`'s `draw()`, after the sprite and the venom/frozen status overlays, once per visible monster per frame. |
| **ctx** | The monster instance. |
| **Kind contract** | The callback observes the moment. Its return value is ignored. |

### The ctx parameter

- The monster instance (a `par_monster` child) whose draw just finished.

> [!IMPORTANT]
> Hot path. This event fires for every visible monster, every frame. Make the callback's first check its cheapest early-exit.

## Usage

```gml
// monster.draw is an EVENT: the return value is ignored.
// You cannot change or stop it here; the return value is ignored.
function overlay_painter_monster_draw(_ctx) {
    // _ctx is the monster instance whose draw just finished.
    // You are inside the world draw pass, after the monster's sprite and
    // status overlays have drawn: draw your overlay here and it lands on
    // top of the monster, in world coordinates.
    // HOT PATH: fires for every visible monster, every frame. Make your
    // first check the cheapest one and get out early when your mod has
    // nothing to do.
}

// inside your latched register function (see Mod Anatomy):
mmapi_on("monster.draw", overlay_painter_monster_draw);
```

## Engine Wiring

- Seam [`monster_draw`](../seams/monster_draw.md) dispatches from `gml/objects/Combat/par_monster.gml`, at the end of the `draw()` method, after the status-overlay block.

## See Also

- [monster.step_begin](monster.step_begin.md) - This hook is the per-frame logic counterpart in begin step.
- [ui.draw_gui](ui.draw_gui.md) - Draw in GUI space instead of world space.
- [monster.spawn](monster.spawn.md) - Move, replace, or cancel a monster before it exists to draw.
