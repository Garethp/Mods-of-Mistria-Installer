# Hook: player.status_effect_cancel

Know when the game cancels a status effect.

`player.status_effect_cancel` is an **event** hook. Register a callback with `mmapi_on`. See [Hooks](../HOOKS.md) for how registration and dispatch work.

## Contract

Fires at the top of `StatusEffectManager.cancel()`, before the effect is looked up or removed. ctx is `{ type, manager }`. Fires even when no such effect is active. Natural expiry does not route through `cancel()`. Observe [player.status_effect_expired](player.status_effect_expired.md) for that.

| | |
| --- | --- |
| **Fires** | At the top of `StatusEffectManager.cancel()`, before the effect is looked up or removed. |
| **ctx** | `{ type, manager }` |
| **Kind contract** | The callback observes the moment. Its return value is ignored. |

### The ctx struct

- `type` - the `StatusEffectId` being cancelled, exactly as passed to `cancel()`.
- `manager` - the `StatusEffectManager` `cancel()` was called on. The class is shared with monsters, so compare against `ARI.status_effects` to scope to the player.

> [!NOTE]
> The emit precedes the lookup, so this event is not proof an effect existed. The game cancels speculatively, and a `cancel()` for an effect that was never registered fires here all the same.

## Usage

```gml
// player.status_effect_cancel is an EVENT: mmapi calls you after it happens.
// You cannot change or stop it here; the return value is ignored.
function effect_ledger_player_status_effect_cancel(_ctx) {
    // _ctx is { type, manager }.
    //   .type    - the StatusEffectId being cancelled. May not be active:
    //              cancel() fires here even when no such effect exists.
    //   .manager - the StatusEffectManager cancel() was called on. Shared
    //              with monsters - scope to the player first.
    if (_ctx.manager != ARI.status_effects) return; // not the player's manager
    // your code here, e.g. log which effects the game cuts short
}

// inside your latched register function (see Mod Anatomy):
mmapi_on("player.status_effect_cancel", effect_ledger_player_status_effect_cancel);
```

## Engine Wiring

- Seam [`player_status_effect_cancel`](../seams/player_status_effect_cancel.md) dispatches from `gml/scripts/Player/StatusEffectManager.gml`, at the head of `cancel()`.

## See Also

- [player.status_effect_expired](player.status_effect_expired.md) - This hook is the disjoint signal for natural expiry, which never routes through `cancel()`.
- [player.status_effect_register](player.status_effect_register.md) - Rewrite a status effect as it registers.
