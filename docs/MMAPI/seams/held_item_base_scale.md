# Engine Fix: held_item_base_scale

Scales the held item's frame offset and sprite origin by the player animation runtime's `scale` field on the base animation path, matching the x axis the engine already scales.

`held_item_base_scale` is an **engine fix**, an anchored edit with no hook behind it. Nothing dispatches. Together with [held_item_modifier_scale](held_item_modifier_scale.md) it makes the held item follow the player animation runtime's `scale` field the way every other component already does. See [Seams](../SEAMS.md).

## Placement

| | |
| --- | --- |
| **File** | `gml/scripts/PlayerAnimationRuntime/PlayerAnimationRuntime.gml` |
| **Locator** | text anchor on the two position arguments of the base animation path's `draw_sprite_ext` in the held-item case of `draw_each_component` |
| **Feeds** | (no hook) |
| **Marker** | `mmapi_held_item_base_scale` |

## The Edit

The held-item case takes the base animation path when no modifier animation claims the slot and the slot's base animation is one of the held-item animations, which is how a held-item animation the database does not list as a modifier plays on every slot. Its `draw_sprite_ext` builds the x position from `x_off` plus `x_scale` times the sum of the frame's x offset and the held sprite's x origin, with the West branch swapping the flipped x origin into the sum before the product, so the x position already scales in full. The origins are the held sprite's `x_offset` and `y_offset`, which `set_held_sprite` defaults from `sprite_get_xoffset` and `sprite_get_yoffset`. The y position adds the frame's y offset and the held sprite's y origin to `draw_y` as unscaled pixels, so at any scale but 1 the item draws at the right size at the wrong height.

The replace multiplies that y sum by `y_scale`, so the y line takes the same shape as the x line above it. The line carries the `mmapi_held_item_base_scale` marker as a trailing comment. The x line is untouched. It already carries `x_scale`, and multiplying the x origin again where the West branch writes it would double it.

With the edit staged, the y position is the scaled copy of its scale-1 value about the draw point, as the x position already was. At scale 1 the multiplication is an exact identity, and no `PlayerAnimationRuntime` instance in vanilla holds an item at any scale but 1, so vanilla play is unchanged.

## See Also

- [held_item_modifier_scale](held_item_modifier_scale.md) - The companion edit for the modifier animation path of the same case, which carries the fuller account of the runtime's `scale` field.
- [game_step_begin_installs](game_step_begin_installs.md) - Another of the catalog's engine fixes, the MMAPI lifecycle root.
