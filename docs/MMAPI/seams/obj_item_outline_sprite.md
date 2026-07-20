# Seam: obj_item_outline_sprite

Reroutes the world-item outline draw to the outline its sibling seam computes.

`obj_item_outline_sprite` is a **text seam** and a **companion edit**: it dispatches nothing itself. It exists for [ui.item_icon](../hooks/ui.item_icon.md). It reroutes one draw argument so the outline computed by [ui_item_icon_obj_item_world](ui_item_icon_obj_item_world.md) is what actually renders. Mod authors never write seams. You register handlers for the hooks they dispatch. See [Seams](../SEAMS.md).

## Placement

| | |
| --- | --- |
| **File** | `gml/objects/objects/obj_item.gml` |
| **Locator** | text anchor on the sprite argument of the world-item outline draw call |
| **Feeds** | [`ui.item_icon`](../hooks/ui.item_icon.md) (no dispatch of its own, the dispatch lives in [`ui_item_icon_obj_item_world`](ui_item_icon_obj_item_world.md)) |
| **Marker** | `outline_sprite` |

## The Edit

A one-token reroute inside `obj_item`'s world draw: the outline draw call's sprite argument changes from `item_data.prototype.icon_sprite_outline` to the `outline_sprite` local. There is no dispatch here. The local it reads is declared and computed by [ui_item_icon_obj_item_world](ui_item_icon_obj_item_world.md): `item_data.get_ui_outline()`, falling back to `item_data.prototype.icon_sprite_outline` when that comes back `undefined`, so the item-level outline wins where one exists, and the prototype field this draw used to read remains the fallback.

That sibling declares `depends_on = ["obj_item_outline_sprite"]`, ordering the pair's application so the rerouted draw and the local that feeds it land together.

## See Also

- [ui.item_icon](../hooks/ui.item_icon.md) - This is the hook this companion edit serves.
- [ui_item_icon_obj_item_world](ui_item_icon_obj_item_world.md) - This is the sibling that filters the world sprite and computes `outline_sprite`.
- [ui_item_icon_live_item](ui_item_icon_live_item.md) - This is the `LiveItem.get_ui_icon()` wrap for the same hook.
