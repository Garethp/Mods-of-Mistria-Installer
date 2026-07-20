# Hook: animal.heart_points

Adjust the heart points a barn animal gains.

`animal.heart_points` is a **filter** hook. Register a callback with `mmapi_filter`. See [Hooks](../HOOKS.md) for how registration and dispatch work.

## Contract

Fires at the top of `Animal.add_heart_points()`, before the delta is applied. The filtered value is the heart points delta. ctx is the `Animal` struct. Return the replacement value, or `undefined` to keep the current value.

This filter is distinct from [npc.heart_points](npc.heart_points.md), which covers villagers rather than barn animals. The two are parallel filters on the same kind of delta.

| | |
| --- | --- |
| **Fires** | At the top of `Animal.add_heart_points()`, before the delta is applied. |
| **Value** | The heart points delta. |
| **ctx** | The `Animal` struct. |
| **Kind contract** | The callback receives the current value and returns a replacement, or `undefined` to keep the current value. |

### The ctx parameter

- ctx - the `Animal` data struct whose hearts are changing (`self` inside `add_heart_points()`).

## Usage

```gml
// animal.heart_points is a FILTER: you receive (value, ctx) and return a
// replacement, or undefined to keep the game's value.
function beloved_barn_animal_heart_points(_value, _ctx) {
    // _value is the heart points delta about to be applied.
    // _ctx is the Animal data struct gaining (or losing) the points.
    if (_value == undefined) return undefined; // test undefined BEFORE anything else
    // your change here, e.g. double every gain:
    // if (_value > 0) return _value * 2;
    return undefined; // undefined = keep the game's value
}

mmapi_filter("animal.heart_points", beloved_barn_animal_heart_points);
```

## Engine Wiring

- Seam [`animal_heart_points`](../seams/animal_heart_points.md) dispatches from `gml/scripts/GameplaySystems/Ranching/Animal.gml`, at the head of `add_heart_points(points)`, filtering `points` before the delta is applied.

## See Also

- [npc.heart_points](npc.heart_points.md) - This hook is the villager counterpart. It adjusts the heart points a villager gains.
- [animal.pet](animal.pet.md) - This hook lets you know when the player pets or puts down an animal.
