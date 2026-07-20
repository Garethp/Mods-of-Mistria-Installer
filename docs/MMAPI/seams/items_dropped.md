# Seam: items_dropped

Announces every drop before the items spawn into the world.

`items_dropped` is a **template seam** (`op = "emit"`). It feeds [items.dropped](../hooks/items.dropped.md). Mod authors never write seams. You register handlers for the hooks they dispatch. See [Seams](../SEAMS.md).

## Placement

| | |
| --- | --- |
| **File** | `gml/scripts/GameplaySystems/Inventory/drop_item.gml` |
| **Locator** | pristine context: after the `array == undefined` early return, before the spawn loop |
| **Op** | `emit` |
| **Feeds** | [`items.dropped`](../hooks/items.dropped.md) |
| **ctx built** | `{ value: value, items: array, x: xx, y: yy, z_offset: z_offset }` |
| **Marker** | `mmapi_items_run_drop_callbacks` |

## The Edit

The generated emit lands in `drop_item()` between the pristine `if array == undefined { return; }` early-out and the `for` loop that spawns each item into the world, so it fires exactly once per real drop, after the drop argument has been normalized into an array and never for the nothing-to-drop case. It calls `mmapi_emit("items.dropped", ...)` in the uniform try/catch shape (catch var `__mmapi_item_drop`).

The ctx maps five engine locals into stable names: `value` is the original drop argument as `drop_item` received it, `items` is the normalized array the spawn loop is about to walk, and `x`, `y`, `z_offset` are the drop position (`xx`, `yy`, `z_offset`). Observation only: the drop proceeds regardless. With zero handlers the emit early-outs on an empty registry, leaving pristine behavior.

## See Also

- [items.dropped](../hooks/items.dropped.md) - This is the hook this seam dispatches.
- [items_give](items_give.md) - This is the filter on items entering the inventory. This seam covers items leaving it for the world.
- [inventory_trash_button](inventory_trash_button.md) - This is the emit for items destroyed outright at the trash button.
