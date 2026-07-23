# Hook: player.essence_delta

Change every essence gain or spend before it applies.

`player.essence_delta` is a **filter** hook. Register a callback with `mmapi_filter`. See [Hooks](../HOOKS.md) for how registration and dispatch work.

## Contract

Fires at the top of `Ari.modify_essence()`, before the delta is applied. The filtered value is the signed essence delta. The ctx is `{ player }`. Return the replacement delta (`0` makes the call a no-op), or `undefined` to keep the current value.

The seam floors the filtered result at `-essence`, so the total never goes negative. Absolute sets (save load, new game, debug) do not route through `modify_essence` and never fire this hook.

| | |
| --- | --- |
| **Fires** | At the top of `Ari.modify_essence(amount_to_add)`, before the delta is applied. |
| **Value** | The signed essence delta: gains positive, spends negative. |
| **ctx** | `{ player }` |
| **Kind contract** | The callback receives the current value and returns a replacement, or `undefined` to keep the current value. The seam floors the result at `-essence`. |

### The ctx struct

- `player` - the `Ari` struct whose essence is changing.

## Usage

```gml
// player.essence_delta is a FILTER: you receive (value, ctx) and return a
// replacement, or undefined to keep the game's value.
function essence_magnet_player_essence_delta(_value, _ctx) {
    // _value is the signed essence delta: positive = gain, negative = spend.
    // The seam floors whatever you return at -essence, so an inflated spend
    // bottoms out at zero total.
    // _ctx is { player }.
    //   .player - the Ari struct whose essence is changing.
    if (_value == undefined) return undefined; // test undefined BEFORE anything else
    if (_value > 0) return _value * 2; // double every essence gain
    return undefined; // undefined = keep spends unchanged
}

mmapi_filter("player.essence_delta", essence_magnet_player_essence_delta);
```

## Engine Wiring

- Seam [`player_essence_delta`](../seams/player_essence_delta.md) dispatches from `gml/scripts/GameplaySystems/Player/Ari.gml`, at the head of `modify_essence(amount_to_add)`, filtering `amount_to_add` and flooring the result at `-essence` before `set_essence` runs.

## See Also

- [player.gold_delta](player.gold_delta.md) - The similar filter point for gold.
- [player.mana_delta](player.mana_delta.md) - The similar filter point for mana.
- [player.health_delta](player.health_delta.md) - The similar filter point for health.
