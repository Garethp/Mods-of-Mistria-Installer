# Seam: ui_item_icon_obj_item_world

Filters the world-drop item sprite and computes the outline its companion draws.

`ui_item_icon_obj_item_world` is a **text seam**, a verbatim `anchor`/`replace` edit. It feeds [ui.item_icon](../hooks/ui.item_icon.md). Mod authors never write seams. You register handlers for the hooks they dispatch. See [Seams](../SEAMS.md).

## Placement

| | |
| --- | --- |
| **File** | `gml/objects/objects/obj_item.gml` |
| **Locator** | text anchor in the world-item draw, right after `var item_data = self.items.first();` and before the infusion outline-color logic |
| **Feeds** | [`ui.item_icon`](../hooks/ui.item_icon.md) |
| **Value filtered** | the world icon, starting as `item_data.get_world_icon()` - may be `undefined` |
| **ctx built** | `{ item: item_data, source: "obj_item_world" }` |
| **Depends on** | [`obj_item_outline_sprite`](obj_item_outline_sprite.md) |
| **Marker** | `__mmapi_world_item_icon` |

## The Edit

Right after the draw code reads `item_data = self.items.first()`, the replace computes `world_sprite = item_data.get_world_icon()` in a try/catch (it may come back, or stay, `undefined`), then filters it through `mmapi_apply_filters("ui.item_icon", world_sprite, { item: item_data, source: "obj_item_world" })` in a second try/catch. When the result is defined and differs from the instance's current `sprite_index`, the seam assigns it and recomputes every draw metric derived from the sprite (`icon_width`, `y_offset`, `tex_h`, `tex_w`) so a swapped sprite renders with correct dimensions. An `undefined` result (no world icon and no handler claim) leaves the instance sprite alone.

The replace then computes `outline_sprite = item_data.get_ui_outline()` in a try/catch, falling back to `item_data.prototype.icon_sprite_outline` when that is `undefined`. That local dispatches nothing further. It exists for [obj_item_outline_sprite](obj_item_outline_sprite.md), the companion edit that reroutes the outline draw call to read it. This seam's `depends_on = ["obj_item_outline_sprite"]` edge orders the pair's application so the reroute and the local that feeds it land together.

## See Also

- [ui.item_icon](../hooks/ui.item_icon.md) - This is the hook this seam dispatches.
- [obj_item_outline_sprite](obj_item_outline_sprite.md) - This is the companion edit (no dispatch) that draws the computed outline.
- [ui_item_icon_live_item](ui_item_icon_live_item.md) - This is the `LiveItem.get_ui_icon()` wrap covering every non-world icon lookup.
