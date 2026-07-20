# Hook: player.status_effect_expired

Know the moment a status effect runs out.

`player.status_effect_expired` is an **event** hook. Register a callback with `mmapi_on`. See [Hooks](../HOOKS.md) for how registration and dispatch work.

## Contract

Fires in `StatusEffectManager.update()` when an effect's finish time lapses, right after the effect is removed from the manager. ctx is `{ type, effect, manager }`: `type` is the `StatusEffectId` map key, `effect` is the removed struct (`{ type, amount, start, finish, stacks, last_update }`), `manager` is the `StatusEffectManager`.

Disjoint from [player.status_effect_cancel](player.status_effect_cancel.md): natural expiry never calls `cancel()`, and `cancel()` never reaches this branch. The manager class is shared with monsters (`par_monster`), so monster-owned expiries fire too. Compare `ctx.manager` against `ARI.status_effects` to scope to the player.

The vitals HUD removes its icon on its own poll up to a frame later, so it may still be visible at emit time. Re-registering the effect from a handler is safe.

| | |
| --- | --- |
| **Fires** | In `StatusEffectManager.update()` when an effect's finish time lapses, right after the effect is removed. |
| **ctx** | `{ type, effect, manager }` |
| **Kind contract** | The callback observes the moment. Its return value is ignored. |

### The ctx struct

- `type` - the `StatusEffectId` map key the effect was registered under, the same id you would pass to `register()` or `cancel()`.
- `effect` - the effect struct just removed from the manager:
  - `effect.type` - the effect's own `StatusEffectId`.
  - `effect.amount` - the effect's magnitude.
  - `effect.start` - when the effect started.
  - `effect.finish` - when the effect was set to expire, the time that just lapsed.
  - `effect.stacks` - the effect's stack count.
  - `effect.last_update` - the manager's last update timestamp for the effect.
- `manager` - The `StatusEffectManager` that owned the effect. This manager is shared with monsters (`par_monster`), so compare against `ARI.status_effects` to scope to the player.

> [!NOTE]
> The vitals HUD icon lags the emit: `VitalsMenu` removes it on its own poll up to a frame later, so do not treat a visible icon as proof the effect is still active.

## Usage

```gml
// player.status_effect_expired is an EVENT: the return value is ignored.
// You cannot change or stop it here; the return value is ignored.
function second_wind_player_status_effect_expired(_ctx) {
    // _ctx is { type, effect, manager }.
    //   .type    - the StatusEffectId map key that expired.
    //   .effect  - the removed struct:
    //              { type, amount, start, finish, stacks, last_update }.
    //   .manager - the StatusEffectManager that owned it. Shared with
    //              monsters - scope to the player first.
    if (_ctx.manager != ARI.status_effects) return; // not the player's manager
    // re-registering from here is safe, e.g. give a buff one encore:
    // _ctx.manager.register(_ctx.type, _ctx.effect.amount, <start>, <finish>);
}

// inside your latched register function (see Mod Anatomy):
mmapi_on("player.status_effect_expired", second_wind_player_status_effect_expired);
```

## Engine Wiring

- Seam [`player_status_effect_expired`](../seams/player_status_effect_expired.md) dispatches from `gml/scripts/Player/StatusEffectManager.gml`, inside `update()`'s expiry branch, immediately after `self.effects.remove(i)` and before the loop `continue`s.

## See Also

- [player.status_effect_cancel](player.status_effect_cancel.md) - This hook is the disjoint signal from an explicit `cancel()`, which fires even when no such effect is active.
- [player.status_effect_register](player.status_effect_register.md) - Rewrite a status effect as it registers (including your own re-registrations from here).
- [ui.menu_refreshed](ui.menu_refreshed.md) - This hook is the vitals menu's status icon strip rebuild, where the lagging icon disappears.
