# Seam: player_stamina_delta

Filters the signed stamina delta before the stamina cost modifier applies.

`player_stamina_delta` is a **template seam** (`op = "filter"`). It feeds [player.stamina_delta](../hooks/player.stamina_delta.md). Mod authors never write seams. You register handlers for the hooks they dispatch. See [Seams](../SEAMS.md).

## Placement

| | |
| --- | --- |
| **File** | `gml/scripts/GameplaySystems/Player/Ari.gml` |
| **Locator** | pristine context: the head of `modify_stamina(amount_to_add)`, before the `set_stamina` line |
| **Op** | `filter` (`try_catch = false`) |
| **Feeds** | [`player.stamina_delta`](../hooks/player.stamina_delta.md) |
| **Value filtered** | `amount_to_add` - the signed stamina delta |
| **ctx built** | `{ player: self }` |
| **Marker** | `mmapi_player_run_stamina_delta_filters` |

## The Edit

The generated dispatch lands at the head of `Ari.modify_stamina()` and reassigns its argument: `amount_to_add = mmapi_apply_filters("player.stamina_delta", amount_to_add, { player: self })`. The engine then applies `set_stamina(o + (amount_to_add * self.stamina_costs_modifier))`, so the filter runs **before** `stamina_costs_modifier` multiplies the delta. A filter sees and replaces the raw pre-modifier value. The player's cost modifier still scales whatever the filter returns.

This seam sets `try_catch = false`: the dispatch is a direct assignment, not wrapped in the uniform try/catch shape. The filter registry's own per-handler isolation still applies. A throwing handler is contained by the registry, not by the seam. `modify_stamina` funnels every stamina change, costs and restores alike, so one edit filters them all.

## See Also

- [player.stamina_delta](../hooks/player.stamina_delta.md) - This is the hook this seam dispatches.
- [player_health_delta](player_health_delta.md) - This seam is the same shape on `modify_health()`.
- [player_move_speed](player_move_speed.md) - This seam is the third of `Ari.gml`'s direct-dispatch stat filters.
