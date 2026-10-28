# Seam: items_treasure_distribution_none

Filters the treasure roll's empty exit so mods can inject a drop where there was none.

`items_treasure_distribution_none` is a **text seam** (`anchor` + `replace`). It feeds [items.treasure_distribution](../hooks/items.treasure_distribution.md). Mod authors never write seams. You register handlers for the hooks they dispatch. See [Seams](../SEAMS.md).

## Placement

| | |
| --- | --- |
| **File** | `gml/scripts/GameplaySystems/Data/Grid/Breakables.gml` |
| **Locator** | text anchor on the `if candidate == undefined { return undefined; }` branch that exits the treasure roll with no candidate |
| **Op** | text (`anchor` + `replace`) |
| **Feeds** | [`items.treasure_distribution`](../hooks/items.treasure_distribution.md) |
| **Value filtered** | `undefined` - the roll produced no candidate |
| **ctx built** | `{ x: xx, y: yy }` |
| **Marker** | `mmapi_items_run_treasure_none_filters` |

## The Edit

The dungeon treasure distribution roll has two exits, and this seam rewrites the empty one. Where pristine code ran `return undefined;` when the roll produced no candidate, the injected block filters that `undefined` through the hook and returns the result. It initializes `__mmapi_treasure_none` to `undefined`, assigns it the result of `mmapi_apply_filters("items.treasure_distribution", undefined, { x: xx, y: yy })` inside a try/catch with the marker as a trailing comment, and returns the local.

A handler that returns a `[live_item, is_perk]` array here injects a drop where the engine rolled none. This is the case where `undefined` is the meaningful, expected value of the hook. When every handler returns `undefined` (or there are none), the branch still returns `undefined`, exactly as pristine. Because the local is initialized to `undefined` before the `try`, a throwing dispatch also leaves the pristine result intact.

Together with [items_treasure_distribution_result](items_treasure_distribution_result.md) at the successful exit, every outcome of the same roll passes through `items.treasure_distribution`.

## See Also

- [items.treasure_distribution](../hooks/items.treasure_distribution.md) - This is the hook this seam dispatches.
- [items_treasure_distribution_result](items_treasure_distribution_result.md) - This is the twin seam at the roll's successful exit, filtering `[candidate_live_item, is_perk]`.
- [dungeon_treasure_chest](dungeon_treasure_chest.md) - This is the other `Breakables.gml` seam, which announces a treasure chest's drop chain.
