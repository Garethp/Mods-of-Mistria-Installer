# Hook: items.treasure_distribution

Change what the dungeon treasure roll drops.

`items.treasure_distribution` is a **filter** hook. Register a callback with `mmapi_filter`. See [Hooks](../HOOKS.md) for how registration and dispatch work.

## Contract

Fires at both exits of the dungeon treasure distribution roll: once when the roll produced no candidate and once on the rolled result. The filtered value is `[live_item, is_perk]`, or `undefined` in the no-candidate case. ctx is `{ x, y }`. Return a replacement `[live_item, is_perk]` array (in the no-candidate case this injects a drop where there was none), or `undefined` to keep the current value.

| | |
| --- | --- |
| **Fires** | At both exits of the dungeon treasure distribution roll. |
| **Value** | `[live_item, is_perk]`, or `undefined` when the roll produced no candidate. |
| **ctx** | `{ x, y }` |
| **Kind contract** | The callback receives the current value and returns a replacement, or `undefined` to keep the current value. |

### The value array

- `[0]` - the rolled `LiveItem`.
- `[1]` - the roll's `is_perk` flag.

### The ctx struct

- `x` - the roll's x position.
- `y` - the roll's y position.

> [!NOTE]
> `undefined` is a meaningful value here: it is the no-candidate exit, not a missing one. Do not open your handler with the usual `if (_value == undefined) return undefined;` early-return. Handle that case deliberately. Returning a replacement array there injects a drop where the roll produced none.

## Usage

```gml
// items.treasure_distribution is a FILTER: you receive (value, ctx) and return
// a replacement, or undefined to keep the game's value.
function generous_dungeon_items_treasure_distribution(_value, _ctx) {
    // _value is [live_item, is_perk], or undefined when the roll produced
    // no candidate. undefined is meaningful here - do not early-return on it.
    // _ctx is { x, y }.
    //   .x, .y - the roll's position.
    if (_value == undefined) {
        // the roll came up empty - inject a consolation drop:
        // return [new LiveItem(<item_id>), false];
        return undefined; // keep the empty result
    }
    // _value[0] is the rolled LiveItem, _value[1] is the is_perk flag.
    return undefined; // undefined = keep the game's roll
}

mmapi_filter("items.treasure_distribution", generous_dungeon_items_treasure_distribution);
```

## Engine Wiring

- Seam [`items_treasure_distribution_none`](../seams/items_treasure_distribution_none.md) dispatches from `gml/scripts/GameplaySystems/Data/Grid/Breakables.gml`, at the no-candidate exit. It filters `undefined`, and a non-`undefined` return replaces the bare `return undefined;`.
- Seam [`items_treasure_distribution_result`](../seams/items_treasure_distribution_result.md) dispatches from the same file at the rolled-result exit, filtering `[candidate_live_item, is_perk]` just before it is returned.

## See Also

- [dungeon.treasure_chest](dungeon.treasure_chest.md) - Know when a treasure chest starts its drop chain.
- [items.dig_artifact](items.dig_artifact.md) - Swap the artifact an archaeology dig spot yields.
- [items.dropped](items.dropped.md) - Know what is about to drop into the world.
