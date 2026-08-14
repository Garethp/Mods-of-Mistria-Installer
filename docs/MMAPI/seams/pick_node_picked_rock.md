# Seam: pick_node_picked_rock

Emits the moment a pick lands on a rock, the chip and the break alike.

`pick_node_picked_rock` is a **template seam** (`op = "emit"`). It feeds [resource.node_picked](../hooks/resource.node_picked.md). Mod authors never write seams. You register handlers for the hooks they dispatch. See [Seams](../SEAMS.md).

## Placement

| | |
| --- | --- |
| **File** | `gml/scripts/GameplaySystems/Data/Grid/GridActions/Pick.gml` |
| **Locator** | pristine context inside `pick_node()`'s Rock case, right after the hitpoints decrement resolves into a `ToolStatus` |
| **Op** | `emit` |
| **Feeds** | [`resource.node_picked`](../hooks/resource.node_picked.md) |
| **ctx built** | `{ grid, x: x_pos, y: y_pos, item, node, result: "rock", destroyed: node.hitpoints <= 0, burn: is_burn, effect_override }` |
| **Marker** | `mmapi_pick_node_picked_rock` |

## The Edit

The emit sits inside the Rock case's quality-pass branch, immediately after the damage is applied and the outcome is stored as `ToolStatus.Damage` or `ToolStatus.Dead`. That single site covers both the chip and the break, and it runs before the `ToolStatus` switch does anything: before drops, mining XP, ore perk procs, Earthbreaker chaining, and before every early return in the Dead branch. An inadequate-quality swing never enters the branch, so `ToolStatus.None` bounces never fire.

Because the site precedes the teardown, `node` in ctx is still fully live on the break. `effect_override` passes the function's argument through, which is non-`undefined` exactly for the engine's internal Earthbreaker chain picks. With zero handlers the emit dispatches to nobody and the pick proceeds untouched.

## See Also

- [resource.node_picked](../hooks/resource.node_picked.md) - This is the hook this seam dispatches.
- [pick_node_picked_dig_site](pick_node_picked_dig_site.md) - This is the twin site in the dig-site success branch.
- [pick_node_modifier](pick_node_modifier.md) - This is the filter at the head of the same function, before the pick resolves.
