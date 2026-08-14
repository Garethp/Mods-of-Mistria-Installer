# Seam: chop_node_chopped_stump

Emits the moment a chop lands on a stump or branch, the damaging hit and the breaking hit alike.

`chop_node_chopped_stump` is a **template seam** (`op = "emit"`). It feeds [resource.node_chopped](../hooks/resource.node_chopped.md). Mod authors never write seams. You register handlers for the hooks they dispatch. See [Seams](../SEAMS.md).

## Placement

| | |
| --- | --- |
| **File** | `gml/scripts/GameplaySystems/Data/Grid/GridActions/Chop.gml` |
| **Locator** | pristine context inside `chop_node()`'s Stump case, between the damaged/destroyed join's closing brace and its `return true;` |
| **Op** | `emit` |
| **Feeds** | [`resource.node_chopped`](../hooks/resource.node_chopped.md) |
| **ctx built** | `{ grid, x: x_pos, y: y_pos, item, node, destroyed: node.hitpoints <= 0, burn: is_burn }` |
| **Marker** | `mmapi_chop_node_chopped_stump` |

## The Edit

The Stump case covers stumps and branches. Like its tree twin, this seam rides the case's damaged/destroyed join so the emit lands after the outcome is fully decided: one site covers both the damaging chip and the breaking hit, with `node.hitpoints <= 0` as the `destroyed` flag. The inadequate-quality bounce and the burn i-frame absorb leave the case before the join and never reach the emit.

On the breaking hit the node's drops and Lumberjack perk procs have already run and the node is erased from the grid, but the `node` struct in ctx keeps its fields readable. With zero handlers the emit dispatches to nobody and the chop proceeds untouched.

## See Also

- [resource.node_chopped](../hooks/resource.node_chopped.md) - This is the hook this seam dispatches.
- [chop_node_chopped_tree](chop_node_chopped_tree.md) - This is the twin site in the Tree case.
- [chop_node_modifier](chop_node_modifier.md) - This is the filter at the head of the same function, before the chop resolves.
