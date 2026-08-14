# Seam: chop_node_chopped_tree

Emits the moment a chop lands on a tree, the damaging hit and the felling hit alike.

`chop_node_chopped_tree` is a **template seam** (`op = "emit"`). It feeds [resource.node_chopped](../hooks/resource.node_chopped.md). Mod authors never write seams. You register handlers for the hooks they dispatch. See [Seams](../SEAMS.md).

## Placement

| | |
| --- | --- |
| **File** | `gml/scripts/GameplaySystems/Data/Grid/GridActions/Chop.gml` |
| **Locator** | pristine context inside `chop_node()`'s Tree case, between the damaged/destroyed join's closing brace and its `return true;` |
| **Op** | `emit` |
| **Feeds** | [`resource.node_chopped`](../hooks/resource.node_chopped.md) |
| **ctx built** | `{ grid, x: x_pos, y: y_pos, item, node, destroyed: node.hitpoints <= 0, burn: is_burn }` |
| **Marker** | `mmapi_chop_node_chopped_tree` |

## The Edit

`chop_node()` returns `true` from exactly two places, one per landed-chop case, each right after that case's damaged/destroyed branches rejoin. This seam rides the Tree case's join: the emit lands after the outcome is fully decided, so one site covers both the damaging chip and the felling hit, and `node.hitpoints <= 0` reads as the `destroyed` flag. The inadequate-quality bounce and the burn i-frame absorb leave the case elsewhere and never reach the emit.

On the felling hit the node is already erased from the grid when the emit runs, but the `node` struct in ctx keeps its fields readable. With zero handlers the emit dispatches to nobody and the chop proceeds untouched.

## See Also

- [resource.node_chopped](../hooks/resource.node_chopped.md) - This is the hook this seam dispatches.
- [chop_node_chopped_stump](chop_node_chopped_stump.md) - This is the twin site in the Stump case.
- [chop_node_modifier](chop_node_modifier.md) - This is the filter at the head of the same function, before the chop resolves.
