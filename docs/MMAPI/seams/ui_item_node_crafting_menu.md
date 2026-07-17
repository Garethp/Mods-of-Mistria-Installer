# Seam: ui_item_node_crafting_menu

Hands each crafting-grid icon node to mods as the menu builds.

`ui_item_node_crafting_menu` is a **template seam** (`op = "filter_call"`, in-place: dispatch called, return discarded). It feeds [ui.item_node](../hooks/ui.item_node.md). Mod authors never write seams. You register handlers for the hooks they dispatch. See [Seams](../SEAMS.md).

## Placement

| | |
| --- | --- |
| **File** | `gml/scripts/UI/Anchor/Menus/CraftingMenu.gml` |
| **Locator** | pristine context in the crafting grid build, after `icon.set_sprite(item.get_ui_icon());` and before `square.set_tap_callback(...)` |
| **Op** | `filter_call` (in-place) |
| **Feeds** | [`ui.item_node`](../hooks/ui.item_node.md) |
| **Value filtered** | `{ node: icon, item: item, count: 1, source: "crafting_menu" }` - the struct rides in the value position |
| **Marker** | `__mmapi_crafting_item_node_filter` |

## The Edit

The crafting menu populates its recipe grid directly (each square's icon gets `icon.set_sprite(item.get_ui_icon())`) rather than through `item_node.set_to_item()`, so the [ui_item_node_set_to_item](ui_item_node_set_to_item.md) dispatch never sees these nodes. This seam adds the hook's second dispatch site right after that sprite assignment: it calls `mmapi_apply_filters("ui.item_node", { node: icon, item: item, count: 1, source: "crafting_menu" }, undefined)` in the uniform try/catch shape and discards the return.

The struct rides in the value position and handlers mutate `node` in place. `source: "crafting_menu"` tells a handler which site fired, and `count` is always `1` here. The grid shows recipes, not stacks.

## See Also

- [ui.item_node](../hooks/ui.item_node.md) - This is the hook this seam dispatches.
- [ui_item_node_set_to_item](ui_item_node_set_to_item.md) - This is the twin dispatch site in the shared `set_to_item` populate method.
- [item_display_description](item_display_description.md) - This is the chokepoint for the tooltip description these nodes never draw.
