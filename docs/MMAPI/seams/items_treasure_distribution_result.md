# Seam: items_treasure_distribution_result

Filters the treasure roll's rolled result on its way out.

`items_treasure_distribution_result` is a **text seam** (`anchor` + `replace`). It feeds [items.treasure_distribution](../hooks/items.treasure_distribution.md). Mod authors never write seams. You register handlers for the hooks they dispatch. See [Seams](../SEAMS.md).

## Placement

| | |
| --- | --- |
| **File** | `gml/scripts/GameplaySystems/Data/Grid/Breakables.gml` |
| **Locator** | text anchor on `return [candidate_live_item, is_perk];`, the treasure roll's successful exit |
| **Op** | text (`anchor` + `replace`) |
| **Feeds** | [`items.treasure_distribution`](../hooks/items.treasure_distribution.md) |
| **Value filtered** | `[candidate_live_item, is_perk]` - the rolled drop and its perk flag |
| **ctx built** | `{ x: xx, y: yy }` |
| **Marker** | `mmapi_items_run_treasure_result_filters` |

## The Edit

The dungeon treasure distribution roll has two exits, and this seam rewrites the successful one. Where pristine code ran `return [candidate_live_item, is_perk];`, the injected block captures the rolled pair into a local, filters it, and returns the result. It initializes `__mmapi_treasure_result` to the pristine `[candidate_live_item, is_perk]` array, assigns it the result of `mmapi_apply_filters("items.treasure_distribution", __mmapi_treasure_result, { x: xx, y: yy })` inside a try/catch with the marker as a trailing comment, and returns the local.

A handler can return a replacement `[live_item, is_perk]` array to swap the drop, or `undefined` to keep the engine's roll. Because the local is initialized with the pristine array *before* the `try`, a throwing dispatch leaves the engine's result intact. The roll survives any handler failure.

Together with [items_treasure_distribution_none](items_treasure_distribution_none.md) at the no-candidate exit, every outcome of the same roll passes through `items.treasure_distribution`, which is why `undefined` is a meaningful value for that hook rather than a sentinel to skip.

## See Also

- [items.treasure_distribution](../hooks/items.treasure_distribution.md) - This is the hook this seam dispatches.
- [items_treasure_distribution_none](items_treasure_distribution_none.md) - This is the twin seam at the roll's empty exit, where a filter can inject a drop from nothing.
- [dungeon_treasure_chest](dungeon_treasure_chest.md) - This is the other `Breakables.gml` seam, which announces a treasure chest's drop chain.
