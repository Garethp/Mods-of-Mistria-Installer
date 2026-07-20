# Hook: npc.heart_points

Adjust the heart points a villager gains.

`npc.heart_points` is a **filter** hook. Register a callback with `mmapi_filter`. See [Hooks](../HOOKS.md) for how registration and dispatch work.

## Contract

Fires at the top of `Npc.add_heart_points()`, before the delta is applied. The filtered value is the heart points delta (`amount`). ctx is the `Npc` struct. Return the replacement value, or `undefined` to keep the current value.

This filter is distinct from [animal.heart_points](animal.heart_points.md), which covers barn animals rather than villagers. The two are parallel filters on the same kind of delta.

| | |
| --- | --- |
| **Fires** | At the top of `Npc.add_heart_points()`, before the delta is applied. |
| **Value** | The heart points delta (`amount`). |
| **ctx** | The `Npc` struct. |
| **Kind contract** | The callback receives the current value and returns a replacement, or `undefined` to keep the current value. |

### The ctx parameter

- ctx - the `Npc` struct whose hearts are changing (`self` inside `add_heart_points()`).

## Usage

```gml
// npc.heart_points is a FILTER: you receive (value, ctx) and return a
// replacement, or undefined to keep the game's value.
function fast_friends_npc_heart_points(_value, _ctx) {
    // _value is the heart points delta about to be applied.
    // _ctx is the Npc struct gaining (or losing) the points.
    if (_value == undefined) return undefined; // test undefined BEFORE anything else
    // your change here, e.g. double every gain:
    // if (_value > 0) return _value * 2;
    return undefined; // undefined = keep the game's value
}

mmapi_filter("npc.heart_points", fast_friends_npc_heart_points);
```

## Engine Wiring

- Seam [`npc_heart_points`](../seams/npc_heart_points.md) dispatches from `gml/scripts/GameplaySystems/NPCs/Npc.gml`, at the head of `add_heart_points(amount)`, filtering `amount` before the delta is applied.

## See Also

- [animal.heart_points](animal.heart_points.md) - This barn-animal counterpart adjusts the heart points a barn animal gains.
- [npc.gift_received](npc.gift_received.md) - Know when the player gives an NPC a gift.
- [gossip.selections](gossip.selections.md) - Change which NPCs the day's gossip offers.
