# Seam: player_health_delta

Filters the signed health delta at the top of `Ari.modify_health()`.

`player_health_delta` is a **template seam** (`op = "filter"`). It feeds [player.health_delta](../hooks/player.health_delta.md). Mod authors never write seams. You register handlers for the hooks they dispatch. See [Seams](../SEAMS.md).

## Placement

| | |
| --- | --- |
| **File** | `gml/scripts/GameplaySystems/Player/Ari.gml` |
| **Locator** | pristine context: the head of `modify_health(amount_to_add, play_sound)`, before the `amount_to_add == 0` early-out |
| **Op** | `filter` (`try_catch = false`) |
| **Feeds** | [`player.health_delta`](../hooks/player.health_delta.md) |
| **Value filtered** | `amount_to_add` - the signed health delta |
| **ctx built** | `{ player: self, play_sound: play_sound }` |
| **Marker** | `mmapi_player_run_health_delta_filters` |

## The Edit

The generated dispatch lands at the head of `Ari.modify_health()` and reassigns its first argument: `amount_to_add = mmapi_apply_filters("player.health_delta", amount_to_add, { player: self, play_sound: play_sound })`. It runs before the function's own `if amount_to_add == 0 { return; }` check, so a filter that returns `0` turns the whole call into a no-op, with no `set_health` and no sound. Any other return replaces the delta the engine goes on to apply via `set_health(o + amount_to_add, play_sound)`.

This seam sets `try_catch = false`: the dispatch is a direct assignment, not wrapped in the uniform try/catch shape. The filter registry's own per-handler isolation still applies. A throwing handler is contained by the registry, not by the seam. `modify_health` is the funnel for every health change (damage, healing, food), so this one edit filters them all.

## See Also

- [player.health_delta](../hooks/player.health_delta.md) - This is the hook this seam dispatches.
- [player_stamina_delta](player_stamina_delta.md) - This seam applies the same shape on `modify_stamina()`.
- [player_incoming_damage](player_incoming_damage.md) - This is the combat-specific filter that decides whether `modify_health` is called at all.
- [player_max_health_item](player_max_health_item.md) - This is the emit that fires when an item raises max health before calling `modify_health`.
