# Hook: player.stamina_delta

Change every stamina cost or gain before it applies.

`player.stamina_delta` is a **filter** hook. Register a callback with `mmapi_filter`. See [Hooks](../HOOKS.md) for how registration and dispatch work.

## Contract

Fires at the top of `Ari.modify_stamina()`, before `stamina_costs_modifier` is applied. The filtered value is the signed stamina delta. ctx is `{ player }`. Return the replacement delta, or `undefined` to keep the current value.

Because the filter runs first, the delta you return is still scaled: the engine then runs `set_stamina(o + (amount_to_add * self.stamina_costs_modifier))`.

| | |
| --- | --- |
| **Fires** | At the top of `Ari.modify_stamina(amount_to_add)`, before `stamina_costs_modifier` is applied. |
| **Value** | The signed stamina delta: negative costs, positive restores. |
| **ctx** | `{ player }` |
| **Kind contract** | The callback receives the current value and returns a replacement, or `undefined` to keep the current value. |

### The ctx struct

- `player` - the `Ari` struct whose stamina is changing.

## Usage

```gml
// player.stamina_delta is a FILTER: you receive (value, ctx) and return a
// replacement, or undefined to keep the game's value.
function tireless_player_stamina_delta(_value, _ctx) {
    // _value is the signed stamina delta: negative = cost, positive = restore.
    // The engine multiplies your replacement by stamina_costs_modifier after.
    // _ctx is { player }.
    //   .player - the Ari struct whose stamina is changing.
    if (_value == undefined) return undefined; // test undefined BEFORE anything else
    if (_value < 0) return _value * 0.5; // half stamina costs
    return undefined; // undefined = keep gains unchanged
}

mmapi_filter("player.stamina_delta", tireless_player_stamina_delta);
```

## Engine Wiring

- Seam [`player_stamina_delta`](../seams/player_stamina_delta.md) dispatches from `gml/scripts/GameplaySystems/Player/Ari.gml`, at the head of `modify_stamina(amount_to_add)`, filtering `amount_to_add` before the engine multiplies it by `stamina_costs_modifier` and calls `set_stamina`.

## See Also

- [player.health_delta](player.health_delta.md) - The similar filter point for health.
- [player.mana_delta](player.mana_delta.md) - The similar filter point for mana.
- [player.move_speed](player.move_speed.md) - Change the player's move speed after every engine modifier.
