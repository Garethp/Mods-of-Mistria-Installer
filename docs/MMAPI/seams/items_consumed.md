# Seam: items_consumed

Announces every item the player eats, right after the stat is recorded.

`items_consumed` is a **template seam** (`op = "emit"`). It feeds [items.consumed](../hooks/items.consumed.md). Mod authors never write seams. You register handlers for the hooks they dispatch. See [Seams](../SEAMS.md).

## Placement

| | |
| --- | --- |
| **File** | `gml/scripts/Player/AriFsm.gml` |
| **Locator** | pristine context: right after the `GAME_STATS.items_eaten` push in the player FSM's eating logic |
| **Op** | `emit` |
| **Feeds** | [`items.consumed`](../hooks/items.consumed.md) |
| **ctx built** | `self.live_item` (the consumed `LiveItem`) |
| **Marker** | `mmapi_items_run_consume_callbacks` |

## The Edit

The generated emit lands in the player FSM's eating logic, immediately after the engine records the meal into `GAME_STATS.items_eaten` (the `array_push` capturing the item's pretty-print, day, hour, and minute). It calls `mmapi_emit("items.consumed", self.live_item)` in the uniform try/catch shape (catch var `__mmapi_item_consume`). ctx is `self.live_item`, the `LiveItem` the player just ate, read from the FSM state that drove the meal.

Observation only: the eat has already happened and been recorded by the time handlers run. With zero handlers the emit early-outs on an empty registry, leaving pristine behavior.

## See Also

- [items.consumed](../hooks/items.consumed.md) - This is the hook this seam dispatches.
- [player_max_health_item](player_max_health_item.md) - This is the other eat-flow seam in `AriFsm.gml`, on max-health-raising items.
- [items_use_guard](items_use_guard.md) - This is the guard that can veto the use before it starts.
