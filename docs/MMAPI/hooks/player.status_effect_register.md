# Hook: player.status_effect_register

Rewrite a status effect as it registers.

`player.status_effect_register` is a **filter** hook. Register a callback with `mmapi_filter`. See [Hooks](../HOOKS.md) for how registration and dispatch work.

## Contract

Fires at the top of `StatusEffectManager.register()`. The filtered value is the struct `{ type, amount, start_time, finish_time, can_stack, show_hud }`. ctx is the manager. Return the replacement struct. The seam tolerates an `undefined` return and re-reads each field defensively, so `undefined` (or a partial struct) keeps the engine's values for whatever is missing.

| | |
| --- | --- |
| **Fires** | At the top of `StatusEffectManager.register()`, before the effect record is built. |
| **Value** | The pending effect struct `{ type, amount, start_time, finish_time, can_stack, show_hud }`. |
| **ctx** | The `StatusEffectManager`. |
| **Kind contract** | The callback receives the current value and returns a replacement, or `undefined` to keep the current value. |

### The value struct

- `type` - the `StatusEffectId` the effect registers under.
- `amount` - the effect's magnitude.
- `start_time` - when the effect starts (`register()`'s `start` argument).
- `finish_time` - when the effect expires (`register()`'s `finish` argument).
- `can_stack` - whether registering the same effect again stacks (engine default `false`).
- `show_hud` - whether the vitals HUD shows an icon for the effect (engine default `true`).

### The ctx parameter

- The `StatusEffectManager` the effect is registering on. The class is shared with monsters, so compare against `ARI.status_effects` to scope to the player.

## Usage

```gml
// player.status_effect_register is a FILTER: you receive (value, ctx) and
// return a replacement, or undefined to keep the game's value.
function potent_brews_player_status_effect_register(_value, _ctx) {
    // _value is the pending effect struct:
    //   .type        - the StatusEffectId to register under.
    //   .amount      - the effect's magnitude.
    //   .start_time  - when the effect starts.
    //   .finish_time - when the effect expires.
    //   .can_stack   - whether re-registering stacks (engine default false).
    //   .show_hud    - whether the vitals HUD shows an icon (engine default true).
    // _ctx is the StatusEffectManager. Shared with monsters - compare
    // against ARI.status_effects to scope to the player.
    if (_value == undefined) return undefined; // test undefined BEFORE anything else
    if (_ctx != ARI.status_effects) return undefined; // player only
    // double every effect's duration:
    _value.finish_time += (_value.finish_time - _value.start_time);
    return _value;
}

mmapi_filter("player.status_effect_register", potent_brews_player_status_effect_register);
```

## Engine Wiring

- Seam [`player_status_effect_register`](../seams/player_status_effect_register.md) dispatches from `gml/scripts/Player/StatusEffectManager.gml`, at the head of `register(type, amount, start, finish, can_stack=false, show_hud=true)`: it builds the struct from the arguments, filters it, and writes each field back in its own try/catch before the engine builds its effect record.

## See Also

- [player.status_effect_expired](player.status_effect_expired.md) - This hook lets you know the moment a status effect runs out.
- [player.status_effect_cancel](player.status_effect_cancel.md) - This hook lets you know when the game cancels a status effect.
- [player.move_speed](player.move_speed.md) - This hook is where several status effect multipliers land.
