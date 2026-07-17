# Hook: items.dig_artifact

Swap the artifact an archaeology dig spot yields.

`items.dig_artifact` is a **filter** hook. Register a callback with `mmapi_filter`. See [Hooks](../HOOKS.md) for how registration and dispatch work.

## Contract

Fires at the return of `Archaeology.choose_random_artifact()`, the artifact an archaeology dig spot yields (overworld locations and dungeon biomes both route through it). The filtered value is the chosen artifact item id. ctx is the `Archaeology` struct. Return the replacement item id, or `undefined` to keep the current value.

| | |
| --- | --- |
| **Fires** | At the return of `Archaeology.choose_random_artifact()`. |
| **Value** | The chosen artifact item id. |
| **ctx** | The `Archaeology` struct. |
| **Kind contract** | The callback receives the current value and returns a replacement, or `undefined` to keep the current value. |

### The ctx parameter

- ctx - the `Archaeology` struct the wrapped `choose_random_artifact()` runs on (`self` inside the method).

## Usage

```gml
// items.dig_artifact is a FILTER: you receive (value, ctx) and return a
// replacement, or undefined to keep the game's value.
function lucky_trowel_items_dig_artifact(_value, _ctx) {
    // _value is the chosen artifact item id.
    // _ctx is the Archaeology struct.
    if (_value == undefined) return undefined; // test undefined BEFORE anything else
    // your change here:
    // return <your_artifact_item_id>;
    return undefined; // undefined = keep the game's value
}

mmapi_filter("items.dig_artifact", lucky_trowel_items_dig_artifact);
```

## Engine Wiring

- Seam [`archaeology_dig_artifact`](../seams/archaeology_dig_artifact.md) dispatches from `gml/scripts/GameplaySystems/Archaeology.gml`, a whole-function wrap of `choose_random_artifact()` that filters its return value.

## See Also

- [items.treasure_distribution](items.treasure_distribution.md) - Change what the dungeon treasure roll drops.
- [dungeon.treasure_chest](dungeon.treasure_chest.md) - Know when a treasure chest starts its drop chain.
