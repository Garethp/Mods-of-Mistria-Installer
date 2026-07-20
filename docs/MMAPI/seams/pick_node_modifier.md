# Seam: pick_node_modifier

Filters the tool modifier at the head of every pick action.

`pick_node_modifier` is a **template seam** (`op = "filter"`). It feeds [resource.node_modifier](../hooks/resource.node_modifier.md). Mod authors never write seams. You register handlers for the hooks they dispatch. See [Seams](../SEAMS.md).

## Placement

| | |
| --- | --- |
| **File** | `gml/scripts/GameplaySystems/Data/Grid/GridActions/Pick.gml` |
| **Locator** | pristine context at the head of `pick_node()`, before `var is_rug_pick = false;` |
| **Op** | `filter` |
| **Feeds** | [`resource.node_modifier`](../hooks/resource.node_modifier.md) |
| **Value filtered** | `modifier` - the tool modifier argument |
| **ctx built** | `{ grid: grid, x: x_pos, y: y_pos, item: item, action: "pick" }` |
| **Marker** | `mmapi_pick_node_modifier` |

## The Edit

The generated dispatch lands at the head of `pick_node(grid, x_pos, y_pos, item, modifier, effect_override, doppel, is_burn=false)`, reassigning the `modifier` argument through `mmapi_apply_filters("resource.node_modifier", modifier, { grid: grid, x: x_pos, y: y_pos, item: item, action: "pick" })` in the uniform try/catch shape, before the tool's modifier is applied to the node. The modifier is the charged-tool bonus/penalty added to `item.damage` and to the `item.quality` gate. Pick covers rocks, forage, and dig sites.

The ctx's `action: "pick"` literal is how a single `resource.node_modifier` handler tells this dispatch site apart from its chop twin: two seams at two function heads feed the one hook. With zero handlers the filter hands `modifier` back unchanged.

## See Also

- [resource.node_modifier](../hooks/resource.node_modifier.md) - This is the hook this seam dispatches.
- [chop_node_modifier](chop_node_modifier.md) - This is the twin seam at the head of `chop_node()`, with `action: "chop"`.
- [combat_tarball_grid](combat_tarball_grid.md) - This is the in-place filter on the swing tarball whose `can_pick_grid_objects` flag gates grid picks upstream.
