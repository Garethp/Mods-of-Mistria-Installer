# Seam: player_max_health_item

Emits right after an item raises the player's base health.

`player_max_health_item` is a **template seam** (`op = "emit"`). It feeds [player.max_health_item](../hooks/player.max_health_item.md). Mod authors never write seams. You register handlers for the hooks they dispatch. See [Seams](../SEAMS.md).

## Placement

| | |
| --- | --- |
| **File** | `gml/scripts/Player/AriFsm.gml` |
| **Locator** | pristine context: after `ARI.base_health += live_item.prototype.max_health_modifier;`, before the vitals menu update |
| **Op** | `emit` |
| **Feeds** | [`player.max_health_item`](../hooks/player.max_health_item.md) |
| **ctx built** | `{ player: ARI, amount: live_item.prototype.max_health_modifier, live_item: live_item }` |
| **Marker** | `mmapi_player_run_max_health_delta_callbacks` |

## The Edit

The generated emit lands in the player FSM's item-use handling, on the line after an item's `max_health_modifier` is added to `ARI.base_health` and before the two follow-ups the engine runs: `ANCHOR.get_menu(Menu.Vitals).set_max_health(ARI.base_health)` and `ARI.modify_health(live_item.prototype.health_modifier, false)`. It calls `mmapi_emit("player.max_health_item", { player: ARI, amount: live_item.prototype.max_health_modifier, live_item: live_item })` in the uniform try/catch shape.

Handlers therefore see `ARI.base_health` already raised but the vitals HUD not yet told about it, and the item's ordinary `health_modifier` heal not yet applied. This is the exact seam between the stat change and its presentation. `amount` is the raise itself, read straight from the item prototype, and `live_item` is the consumed item.

## See Also

- [player.max_health_item](../hooks/player.max_health_item.md) - This is the hook this seam dispatches.
- [items_consumed](items_consumed.md) - This is the same file's general item-consumption emit.
- [player_health_delta](player_health_delta.md) - This seam filters the `modify_health` call that follows this emit.
