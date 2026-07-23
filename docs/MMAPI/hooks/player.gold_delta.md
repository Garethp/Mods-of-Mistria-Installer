# Hook: player.gold_delta

Change every gold gain or spend before it applies.

`player.gold_delta` is a **filter** hook. Register a callback with `mmapi_filter`. See [Hooks](../HOOKS.md) for how registration and dispatch work.

## Contract

Fires at the top of `Ari.modify_gold()`, before the delta is applied. The filtered value is the signed gold delta. The ctx is `{ player }`. Return the replacement delta (`0` makes the call a no-op), or `undefined` to keep the current value.

End-of-day shipping income routes through here too, as one delta when the earnings pay out. Absolute sets (save load, new game, debug) do not route through `modify_gold` and never fire this hook.

| | |
| --- | --- |
| **Fires** | At the top of `Ari.modify_gold(amount_to_add)`, before the delta is applied. |
| **Value** | The signed gold delta: earnings positive, spends negative. |
| **ctx** | `{ player }` |
| **Kind contract** | The callback receives the current value and returns a replacement, or `undefined` to keep the current value. |

### The ctx struct

- `player` - the `Ari` struct whose gold is changing.

## Usage

```gml
// player.gold_delta is a FILTER: you receive (value, ctx) and return a
// replacement, or undefined to keep the game's value.
function hard_times_player_gold_delta(_value, _ctx) {
    // _value is the signed gold delta: positive = earnings, negative = spend.
    // A store checkout is one negative delta of the whole basket total.
    // _ctx is { player }.
    //   .player - the Ari struct whose gold is changing.
    if (_value == undefined) return undefined; // test undefined BEFORE anything else
    if (_value > 0) return _value * 0.5; // half earnings (set_gold truncates the total)
    return undefined; // undefined = keep spends unchanged
}

mmapi_filter("player.gold_delta", hard_times_player_gold_delta);
```

## Engine Wiring

- Seam [`player_gold_delta`](../seams/player_gold_delta.md) dispatches from `gml/scripts/GameplaySystems/Player/Ari.gml`, at the head of `modify_gold(amount_to_add)`, filtering `amount_to_add` before the engine's `set_gold` floors and applies the total.

## See Also

- [player.essence_delta](player.essence_delta.md) - The filter point for essence.
- [player.mana_delta](player.mana_delta.md) - The filter point for mana.
- [store.item_added](store.item_added.md) - Know each item as it lands in the basket this checkout total comes from.
