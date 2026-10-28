# Engine Fix: held_item_modifier_scale

Scales the held item's frame offset and sprite origin by the player animation runtime's `scale` field on the modifier animation path, so the item stays in place at any scale instead of only at 1.

`held_item_modifier_scale` is an **engine fix**, an anchored edit with no hook behind it. Nothing dispatches. Together with [held_item_base_scale](held_item_base_scale.md) it makes the held item follow the player animation runtime's `scale` field the way every other component already does. See [Seams](../SEAMS.md).

## Placement

| | |
| --- | --- |
| **File** | `gml/scripts/PlayerAnimationRuntime/PlayerAnimationRuntime.gml` |
| **Locator** | text anchor on the modifier animation path of the held-item case in `draw_each_component`, from the `x_offset` and `y_offset` locals through the position arguments of its `draw_sprite_ext` |
| **Feeds** | (no hook) |
| **Marker** | `mmapi_held_item_modifier_scale` |

## The Edit

`draw_each_component` derives `x_scale` and `y_scale` from the runtime's `scale` field at its top, negating `x_scale` for the West cardinal, and passes both to every component's `draw_sprite_ext`. The held-item case takes the modifier animation path when the slot's modifier animation is one of the held-item animations, which is how a held-item animation the database lists as a modifier layers over the base animation. The path builds the item's position from the slot's modifier frame offset corrected by the left arm slot's base and modifier frame offsets, then adds the held sprite's own origin offsets so that the sprite's top-left corner lands on that position. The origins are the held sprite's `x_offset` and `y_offset`, which `set_held_sprite` defaults from `sprite_get_xoffset` and `sprite_get_yoffset`. The x frame offset already sits inside an `x_scale` product, and the West branch writes the x origin as `x_scale` times `flipped_x_offset`. The y frame offset, the non-West x origin, and the y origin are added as unscaled pixels. `draw_sprite_ext` scales a sprite around its origin, so at any scale but 1 the item draws at the right size in the wrong place, and the error grows with the origin offsets and with the distance of the scale from 1.

The replace multiplies the y frame offset local by `y_scale`, multiplies the non-West x origin by `x_scale`, and multiplies the y origin by `y_scale` at both places it is added, the held-animal render callback call and the `draw_sprite_ext` call. Each of the four lines carries the `mmapi_held_item_modifier_scale` marker as a trailing comment. The x frame offset and the West branch's x origin are untouched. Both already carry `x_scale`, and multiplying either again would double it. The held-animal render callback receives the corrected position and is otherwise unchanged.

With the edit staged, every term of the item's position carries the scale, so the item's pixels land at their scale-1 position multiplied by the scale about the draw point, which is where every other component's pixels land. At scale 1 each multiplication is an exact identity. No `PlayerAnimationRuntime` instance in vanilla holds an item at any scale but 1. Nothing in the engine writes the `scale` of the runtime the player object holds, and the runtime the customization menu builds through `create_ari_node`, which runs at 2, never receives a held sprite, so vanilla play is unchanged.

## See Also

- [held_item_base_scale](held_item_base_scale.md) - The companion edit for the base animation path of the same case.
- [game_step_begin_installs](game_step_begin_installs.md) - Another of the catalog's engine fixes, the MMAPI lifecycle root.
