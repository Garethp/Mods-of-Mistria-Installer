# Seam: player_status_effect_cancel

Emits at the head of `StatusEffectManager.cancel()`, before any lookup.

`player_status_effect_cancel` is a **template seam** (`op = "emit"`). It feeds [player.status_effect_cancel](../hooks/player.status_effect_cancel.md). Mod authors never write seams. You register handlers for the hooks they dispatch. See [Seams](../SEAMS.md).

## Placement

| | |
| --- | --- |
| **File** | `gml/scripts/Player/StatusEffectManager.gml` |
| **Locator** | structural target: `cancel`, at head |
| **Op** | `emit` |
| **Feeds** | [`player.status_effect_cancel`](../hooks/player.status_effect_cancel.md) |
| **ctx built** | `{ type: type, manager: self }` |
| **Marker** | `mmapi_player_run_status_cancel_callbacks` |

## The Edit

The generated emit lands at the head of `StatusEffectManager.cancel()`, before the effect is looked up or removed. It calls `mmapi_emit("player.status_effect_cancel", { type: type, manager: self })` in the uniform try/catch shape. Because the dispatch precedes the lookup, it fires on every `cancel()` call, even when no effect of that `type` is currently active, so handlers observing it see intent, not confirmed removal. Check `ctx.manager` if you need to know whether the effect actually existed.

Natural expiry never routes through `cancel()`: an effect that times out is removed in `update()` and announced by [player_status_effect_expired](player_status_effect_expired.md) instead. The two emits are disjoint. The locator is structural (function + head), immune to whitespace and comment drift. With zero handlers the emit early-outs on an empty registry.

## See Also

- [player.status_effect_cancel](../hooks/player.status_effect_cancel.md) - This is the hook this seam dispatches.
- [player_status_effect_expired](player_status_effect_expired.md) - This is the disjoint natural-expiry emit in the same file.
- [player_status_effect_register](player_status_effect_register.md) - This is the filter at the effect's front door.
