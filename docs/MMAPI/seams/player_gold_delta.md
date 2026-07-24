# Seam: player_gold_delta

Filters the signed gold delta at the top of `Ari.modify_gold()`.

`player_gold_delta` is a **template seam** (`op = "filter"`). It feeds [player.gold_delta](../hooks/player.gold_delta.md). Mod authors never write seams. You register handlers for the hooks they dispatch. See [Seams](../SEAMS.md).

## Placement

| | |
| --- | --- |
| **File** | `gml/scripts/GameplaySystems/Player/Ari.gml` |
| **Locator** | pristine context: the head of `modify_gold(amount_to_add)`, before the `set_gold` line |
| **Op** | `filter` (`try_catch = false`) |
| **Feeds** | [`player.gold_delta`](../hooks/player.gold_delta.md) |
| **Value filtered** | `amount_to_add` - the signed gold delta |
| **ctx built** | `{ player: self }` |
| **Marker** | `mmapi_player_run_gold_delta_filters` |

## The Edit

The generated dispatch lands at the head of `Ari.modify_gold()` and reassigns its argument: `amount_to_add = mmapi_apply_filters("player.gold_delta", amount_to_add, { player: self })`. The engine then runs `set_gold(self.gold + amount_to_add)`, which floors the resulting total at `0` and truncates fractions - so unlike its essence sibling, this seam needs no floor of its own. An overdrawing replacement bottoms out at broke, exactly as a vanilla overdraw would.

This seam sets `try_catch = false`: the dispatch is a direct assignment, not wrapped in the uniform try/catch shape. The filter registry's own per-handler isolation still applies. A throwing handler is contained by the registry, not by the seam. `modify_gold` funnels every gameplay gold change - sales, rewards, grants, purchases, fees, craft costs, the end-of-day shipping payout - so one edit filters them all. Absolute sets (save load, new game, debug) write through `set_gold` directly and never pass through here.

## See Also

- [player.gold_delta](../hooks/player.gold_delta.md) - This is the hook this seam dispatches.
- [player_essence_delta](player_essence_delta.md) - The same funnel for essence, where the engine has no floor and the seam supplies one.
- [player_mana_delta](player_mana_delta.md) - This seam is the same shape on `modify_mana()`.
