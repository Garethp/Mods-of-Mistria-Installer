# Seam: player_mana_delta

Filters the signed mana delta at the top of `Ari.modify_mana()`.

`player_mana_delta` is a **template seam** (`op = "filter"`). It feeds [player.mana_delta](../hooks/player.mana_delta.md). Mod authors never write seams. You register handlers for the hooks they dispatch. See [Seams](../SEAMS.md).

## Placement

| | |
| --- | --- |
| **File** | `gml/scripts/GameplaySystems/Player/Ari.gml` |
| **Locator** | pristine context: the head of `modify_mana(amount_to_add)`, before the `get_mana`/`set_mana` lines |
| **Op** | `filter` (`try_catch = false`) |
| **Feeds** | [`player.mana_delta`](../hooks/player.mana_delta.md) |
| **Value filtered** | `amount_to_add` - the signed mana delta |
| **ctx built** | `{ player: self }` |
| **Marker** | `mmapi_player_run_mana_delta_filters` |

## The Edit

The generated dispatch lands at the head of `Ari.modify_mana()` and reassigns its argument: `amount_to_add = mmapi_apply_filters("player.mana_delta", amount_to_add, { player: self })`. The engine then runs `set_mana(o + amount_to_add)`, which clamps the resulting total to `[0, mana_max]` - so the seam needs no floor of its own and any replacement is safe.

This seam sets `try_catch = false`: the dispatch is a direct assignment, not wrapped in the uniform try/catch shape. The filter registry's own per-handler isolation still applies. A throwing handler is contained by the registry, not by the seam. `modify_mana` carries the cast-state deductions (already filtered by [spells.cost](../hooks/spells.cost.md)), the new-day point, date bonuses, and dungeon pickups; the one gameplay delta that bypasses it - the mana potion - is rerouted here by the companion seam [player_mana_item_delta](player_mana_item_delta.md). Cutscene absolute sets and save load call `set_mana` directly and never pass through.

## See Also

- [player.mana_delta](../hooks/player.mana_delta.md) - This is the hook this seam dispatches.
- [player_mana_item_delta](player_mana_item_delta.md) - The companion reroute that brings the mana potion into this funnel.
- [spells_cost_fsm_default](spells_cost_fsm_default.md) - One of the cost filters whose output arrives here as a deduction.
