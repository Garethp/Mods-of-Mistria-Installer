# Seam: player_move_speed

Filters the player's computed move speed after the status-effect multipliers.

`player_move_speed` is a **template seam** (`op = "filter"`). It feeds [player.move_speed](../hooks/player.move_speed.md). Mod authors never write seams. You register handlers for the hooks they dispatch. See [Seams](../SEAMS.md).

## Placement

| | |
| --- | --- |
| **File** | `gml/scripts/GameplaySystems/Player/Ari.gml` |
| **Locator** | pristine context: after the `MineTime` / `SlimeDash` / `KillHaste` status-effect multipliers, before `return spd;` |
| **Op** | `filter` (`try_catch = false`) |
| **Feeds** | [`player.move_speed`](../hooks/player.move_speed.md) |
| **Value filtered** | `spd` - the computed move speed |
| **ctx built** | `{ player: self, on_mount: on_mount }` |
| **Marker** | `mmapi_player_run_move_speed_filters` |

## The Edit

The generated dispatch lands at the very end of the player's move speed computation, after the last three status-effect multipliers have been folded in (`spd *= self.status_effects.get_effect_value(...)` for `StatusEffectId.MineTime`, `SlimeDash`, and `KillHaste`) and immediately before `return spd;`. It reassigns `spd = mmapi_apply_filters("player.move_speed", spd, { player: self, on_mount: on_mount })`, so a filter sees the fully computed speed (base, mount state, and every status effect already applied) and its return is exactly what the function hands back to movement code.

This seam sets `try_catch = false`: the dispatch is a direct assignment, not wrapped in the uniform try/catch shape. The filter registry's own per-handler isolation still applies. A throwing handler is contained by the registry, not by the seam.

## See Also

- [player.move_speed](../hooks/player.move_speed.md) - This is the hook this seam dispatches.
- [player_health_delta](player_health_delta.md) - This is a sibling `Ari.gml` direct-dispatch filter.
- [player_stamina_delta](player_stamina_delta.md) - This is a sibling `Ari.gml` direct-dispatch filter.
