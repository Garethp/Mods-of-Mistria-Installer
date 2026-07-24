# Seam: player_essence_delta

Filters the signed essence delta at the top of `Ari.modify_essence()`, floored so the total never goes negative.

`player_essence_delta` is a **text seam** (`anchor` + `replace`). It feeds [player.essence_delta](../hooks/player.essence_delta.md). Mod authors never write seams. You register handlers for the hooks they dispatch. See [Seams](../SEAMS.md).

## Placement

| | |
| --- | --- |
| **File** | `gml/scripts/GameplaySystems/Player/Ari.gml` |
| **Locator** | text anchor on the whole of `modify_essence(amount_to_add)`, a one-line body |
| **Op** | text (`anchor` + `replace`) |
| **Feeds** | [`player.essence_delta`](../hooks/player.essence_delta.md) |
| **Value filtered** | `amount_to_add` - the signed essence delta |
| **ctx built** | `{ player: self }` |
| **Marker** | `mmapi_player_run_essence_delta_filters` |

## The Edit

A text seam rather than a template one, because the dispatch carries its own floor. The replacement runs `mmapi_apply_filters("player.essence_delta", amount_to_add, { player: self })` in a try/catch, and only adopts the result when it is numeric (`is_real` or `is_int64`); the adopted value is floored at `-self.essence` before the pristine `set_essence(self.essence + amount_to_add)` line runs.

The floor is the point of the seam. `set_essence` has no lower clamp, and it carries an `assert(essence >= 0)` on the *pre-change* value - so a handler that overdraws essence does not crash at the overdraw, it arms a crash that fires on the next essence change, far from the handler that caused it. The floor makes that state unreachable. It never binds on engine values: every vanilla spend is affordability-gated before `modify_essence` is called, so with zero handlers the edit is behaviorally equivalent to pristine. The type guard serves the same defensive role as the defensive re-reads in the struct-filter seams: a non-numeric handler return is dropped instead of crashing the arithmetic below.

`modify_essence` funnels every gameplay essence change - morsel pickups, item gains, ritual refunds, shrine offerings, craft costs - so one edit filters them all. Absolute sets (save load, new game, debug) write the field directly and never pass through here.

## See Also

- [player.essence_delta](../hooks/player.essence_delta.md) - This is the hook this seam dispatches.
- [player_xp_delta](player_xp_delta.md) - This seam's sibling: the other delta filter that carries its own floor.
- [player_stamina_delta](player_stamina_delta.md) - The template-form shape this family started from.
