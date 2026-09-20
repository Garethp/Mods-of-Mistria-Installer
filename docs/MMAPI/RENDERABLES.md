# Renderables

[← MMAPI](MMAPI.md)

A renderable is the engine's unit of attached drawing. An instance creates one, keeps a reference to it, and writes its fields. The engine draws it every frame at the position and depth those fields hold, interpolated between steps, with no draw code in GML. The engine attaches its own visuals this way (a monster's status overlay, a world item's outline, an interactable's speech bubble), and a mod attaches a visual to an instance the same way.

The renderable API is engine-native. Nothing in the GML tree defines it, so the compile check cannot tell a real field from a misspelled one, and a wrong name fails only at runtime. This page covers the fields and behaviour verified on the current build.

## Creating One

`create_renderable` is a method on every instance. Its argument is the sprite to show, or `undefined` for a renderable that shows nothing until `animation` is set. The return value is the renderable. Keep it on the owning instance under a name that carries the mod's namespace, so later frames can find it.

```gml
// a handler that receives an instance as _ctx, creating the marker once
function my_mod_mark_instance(_ctx) {
    if (_ctx[$ "__my_mod_marker"] != undefined) { return; }
    // a sprite the mod ships resolves by name, undefined when it is missing
    var sprite = try_string_to_asset("spr_my_mod_marker");
    if (typeof(sprite) == "undefined") { return; }
    var marker = _ctx.create_renderable(sprite);
    marker.speed = 0;
    marker.x_scale = 1;
    marker.y_scale = 1;
    marker.tint = c_white;
    marker.alpha = 1;
    _ctx.__my_mod_marker = marker;
}
```

## Writing Fields

A new renderable starts with the state of the instance that created it, not with neutral values. A renderable created on a monster that faces left starts mirrored, because `x_scale` copies the owner's `image_xscale`. Write every field the visual relies on before the first frame it shows, and write the ones that move on every frame after.

| Field | Meaning |
| --- | --- |
| `animation` | The sprite to draw. `undefined` draws nothing while keeping the renderable alive. |
| `animation_frame` | The frame of that sprite to show. |
| `speed` | The animation speed. `0` holds `animation_frame` still. |
| `x`, `y` | The world position the sprite's origin is drawn at. The engine interpolates it between steps. |
| `x_scale`, `y_scale` | The scale factors. A 1 by 1 sprite scaled to `w` by `h` covers that many pixels, which is how a bar or a box is drawn. |
| `tint` | The color multiplied into the sprite. `c_white` leaves it unchanged. |
| `alpha` | The opacity, from `0` to `1`. |
| `depth` | The draw depth. A lower depth draws in front, so the owner's `depth` minus one draws just in front of the owner. |
| `visible` | Whether the engine draws it at all. |

## Timing

Write the fields after the engine has finished the owner's own writes for the frame, so the visual sits where the owner is drawn. For monsters that moment is [monster.step_end](hooks/monster.step_end.md). A renderable positioned from [monster.step_begin](hooks/monster.step_begin.md) trails the sprite by one step, because the monster moves during its step.

A step handler is not a draw pass. `draw_sprite_ext` and the other draw natives throw outside a draw event on the current build, with the message that `DrawState` is only available in draw events.

The one draw pass a hook exposes is [ui.draw_gui](hooks/ui.draw_gui.md), which draws in screen space with no world position or depth, so it suits a HUD element and not a visual attached to an instance. A renderable is how a step handler puts something on screen in the world.

## Lifetime

`free()` on a renderable ends it. The engine also frees every renderable an instance owns when it destroys that instance, so a renderable meant to live exactly as long as its owner needs no cleanup of its own. Free one explicitly when the visual should end before the owner does, and clear the reference so later frames do not write to it.
