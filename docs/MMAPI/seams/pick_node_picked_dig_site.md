# Seam: pick_node_picked_dig_site

Emits the moment a dig site is successfully dug.

`pick_node_picked_dig_site` is a **template seam** (`op = "emit"`). It feeds [resource.node_picked](../hooks/resource.node_picked.md). Mod authors never write seams. You register handlers for the hooks they dispatch. See [Seams](../SEAMS.md).

## Placement

| | |
| --- | --- |
| **File** | `gml/scripts/GameplaySystems/Data/Grid/GridActions/Pick.gml` |
| **Locator** | pristine context inside `pick_node()`'s DigSite case, in the success branch between the destroyed-sfx `play_good` and the `return PickResult.DigSite;` |
| **Op** | `emit` |
| **Feeds** | [`resource.node_picked`](../hooks/resource.node_picked.md) |
| **ctx built** | `{ grid, x: x_pos, y: y_pos, item, node: grid.node_parent[inst_index], result: "dig_site", destroyed: grid.node_object_id[inst_index] != ObjectId.DigSite, burn: is_burn, effect_override }` |
| **Marker** | `mmapi_pick_node_picked_dig_site` |

## The Edit

The DigSite case only reaches this branch when `dig_site_attempt_dig()` reported success, so the emit fires exactly for landed digs: already-destroyed sites and failed attempts return `PickResult.Nothing` earlier and never reach it. Because the dig has already run, `node` and `destroyed` in ctx read the post-dig state. The node may already be swapped or absent, and `destroyed` reads `true` when the cell no longer holds a diggable `ObjectId.DigSite`.

With zero handlers the emit dispatches to nobody and the dig proceeds untouched.

## See Also

- [resource.node_picked](../hooks/resource.node_picked.md) - This is the hook this seam dispatches.
- [pick_node_picked_rock](pick_node_picked_rock.md) - This is the twin site in the Rock case.
- [archaeology_dig_artifact](archaeology_dig_artifact.md) - This is the filter on which artifact a dig yields.
