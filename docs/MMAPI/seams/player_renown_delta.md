# Seam: player_renown_delta

Filters the renown delta at the top of `Ari.modify_renown()`.

`player_renown_delta` is a **template seam** (`op = "filter"`). It feeds [player.renown_delta](../hooks/player.renown_delta.md). Mod authors never write seams. You register handlers for the hooks they dispatch. See [Seams](../SEAMS.md).

## Placement

| | |
| --- | --- |
| **File** | `gml/scripts/GameplaySystems/Player/Ari.gml` |
| **Locator** | pristine context: the head of `modify_renown(amount_to_add)`, before the `set_renown` line |
| **Op** | `filter` (`try_catch = false`) |
| **Feeds** | [`player.renown_delta`](../hooks/player.renown_delta.md) |
| **Value filtered** | `amount_to_add` - the renown delta |
| **ctx built** | `{ player: self }` |
| **Marker** | `mmapi_player_run_renown_delta_filters` |

## The Edit

The generated dispatch lands at the head of `Ari.modify_renown()` and reassigns its argument: `amount_to_add = mmapi_apply_filters("player.renown_delta", amount_to_add, { player: self })`. The engine then runs `set_renown(self.renown + amount_to_add)`, which floors and clamps the resulting total to `[0, max renown]`, so the seam needs no floor of its own and any replacement is safe. `set_renown` grants level rewards only on the way up (a non-positive level change returns before the reward loop), so a deducting filter lowers the total and the computed level without revoking anything or playing a celebration.

This seam sets `try_catch = false`. The dispatch is a direct assignment, not wrapped in the uniform try/catch shape. The filter registry's own per-handler isolation still applies. A throwing handler is contained by the registry, not by the seam. `modify_renown` has exactly one gameplay caller - the `on_new_day` drain of `pending_renown_entries`, one call per entry (quest completions, museum donations, the prior day's shipping gold) - so the filter sees every renown gain as it lands at day rollover. Direct `set_renown` calls (incuding a new game) are absolutes, so none pass through here.

## See Also

- [player.renown_delta](../hooks/player.renown_delta.md) - This is the hook this seam dispatches.
- [player_gold_delta](player_gold_delta.md) - This seam is the same shape on `modify_gold()`.
- [player_xp_delta](player_xp_delta.md) - The other progression delta, where the engine has no floor and the seam supplies one.
