# Seam: ui_item_node_set_to_item

Hands every populated UI item node to mods, right after its icon is set.

`ui_item_node_set_to_item` is a **template seam** (`op = "filter_call"`, in-place: dispatch called, return discarded). It feeds [ui.item_node](../hooks/ui.item_node.md). Mod authors never write seams. You register handlers for the hooks they dispatch. See [Seams](../SEAMS.md).

## Placement

| | |
| --- | --- |
| **File** | `gml/scripts/UI/Anchor/anchor_utils.gml` |
| **Locator** | pristine context inside `item_node.set_to_item(item, count)`, after `self.set_sprite(item.get_ui_icon())` and before the `if count <= 1` count-label branch |
| **Op** | `filter_call` (in-place) |
| **Feeds** | [`ui.item_node`](../hooks/ui.item_node.md) |
| **Value filtered** | `{ node: self, item: item, count: count }` - the struct rides in the value position |
| **Marker** | `mmapi_ui_run_item_node_filters` |

## The Edit

`set_to_item` is the shared populate method UI item nodes go through. The generated dispatch calls `mmapi_apply_filters("ui.item_node", { node: self, item: item, count: count }, undefined)` in the uniform try/catch shape and discards the return: this is an in-place dispatch, so the struct rides in the value position (the second argument is `undefined`) and handlers mutate `node` (the live item node) directly. A returned replacement would land nowhere.

It runs after the node's sprite has been set from `item.get_ui_icon()` and before the count-label logic, so a handler sees the node fully iconed and can restyle it before the stack count renders.

## See Also

- [ui.item_node](../hooks/ui.item_node.md) - This is the hook this seam dispatches.
- [ui_item_node_crafting_menu](ui_item_node_crafting_menu.md) - This is the twin dispatch site in the crafting grid.
- [ui_item_icon_live_item](ui_item_icon_live_item.md) - This seam filters the icon sprite itself, upstream of this node.
