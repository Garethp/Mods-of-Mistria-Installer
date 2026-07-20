# Seam: player_status_effect_expired

Emits inside `update()`'s expiry branch, right after the effect is removed.

`player_status_effect_expired` is a **template seam** (`op = "emit"`). It feeds [player.status_effect_expired](../hooks/player.status_effect_expired.md). Mod authors never write seams. You register handlers for the hooks they dispatch. See [Seams](../SEAMS.md).

## Placement

| | |
| --- | --- |
| **File** | `gml/scripts/Player/StatusEffectManager.gml` |
| **Locator** | pristine context: inside `update()`'s `if now >= effect.finish {` branch, after `self.effects.remove(i);`, before the `continue` |
| **Op** | `emit` |
| **Feeds** | [`player.status_effect_expired`](../hooks/player.status_effect_expired.md) |
| **ctx built** | `{ type: i, effect: effect, manager: self }` |
| **Marker** | `mmapi_player_run_status_expired_callbacks` |

## The Edit

The generated emit lands in `StatusEffectManager.update()`'s expiry branch: when an effect's finish time lapses (`now >= effect.finish`), the engine removes it with `self.effects.remove(i)`, and the emit fires on the very next line, before the loop's `continue`. It calls `mmapi_emit("player.status_effect_expired", { type: i, effect: effect, manager: self })` in the uniform try/catch shape. `type` is the `StatusEffectId` map key `i` the update loop is iterating, `effect` is the just-removed struct (`{ type, amount, start, finish, stacks, last_update }`), and `manager` is the `StatusEffectManager`.

Because the effect is already removed at emit time, re-registering it from a handler is safe. Two scoping notes carry over from the hook contract: the manager class is shared with monsters (`par_monster`), so monster-owned expiries fire through this same seam (compare `ctx.manager` against `ARI.status_effects` to scope to the player) and the vitals HUD removes its icon on its own poll up to a frame later, so the icon may still be visible at emit time. Cancellation never reaches this branch. That is [player_status_effect_cancel](player_status_effect_cancel.md)'s emit.

## See Also

- [player.status_effect_expired](../hooks/player.status_effect_expired.md) - This is the hook this seam dispatches.
- [player_status_effect_cancel](player_status_effect_cancel.md) - This is the disjoint early-cancel emit in the same file.
- [player_status_effect_register](player_status_effect_register.md) - This is the filter at the effect's front door.
