# Hook: items.dropped

Know what is about to drop into the world.

`items.dropped` is an **event** hook. Register a callback with `mmapi_on`. See [Hooks](../HOOKS.md) for how registration and dispatch work.

## Contract

Fires in `drop_item()` before the dropped items spawn into the world. ctx is `{ value, items, x, y, z_offset }`, where `items` is the array of items to drop and `value` is the original drop argument.

| | |
| --- | --- |
| **Fires** | In `drop_item()`, before the dropped items spawn into the world. |
| **ctx** | `{ value, items, x, y, z_offset }` |
| **Kind contract** | The callback observes the moment. Its return value is ignored. |

### The ctx struct

- `value` - the original drop argument `drop_item()` received.
- `items` - the array of items about to spawn. It is the engine's coerced array, and the emit sits after the `array == undefined` early-out, so it is always an array here.
- `x` - the world x position of the drop.
- `y` - the world y position of the drop.
- `z_offset` - the vertical offset the drops spawn with.

## Usage

```gml
// items.dropped is an EVENT: the return value is ignored.
// You cannot change or stop it here; the return value is ignored.
function magpie_items_dropped(_ctx) {
    // _ctx is { value, items, x, y, z_offset }.
    //   .value    - the original argument drop_item() received.
    //   .items    - the array of items about to spawn.
    //   .x, .y    - the world drop position.
    //   .z_offset - the vertical spawn offset.
    // your code here
}

// inside your latched register function (see Mod Anatomy):
mmapi_on("items.dropped", magpie_items_dropped);
```

## Engine Wiring

- Seam [`items_dropped`](../seams/items_dropped.md) dispatches from `gml/scripts/GameplaySystems/Inventory/drop_item.gml`, after the `array == undefined` early-out and immediately before the spawn loop.

## See Also

- [items.give](items.give.md) - Rewrite any item the player is about to receive.
- [items.trashed](items.trashed.md) - Know the moment the player trashes an item.
- [items.treasure_distribution](items.treasure_distribution.md) - Change what the dungeon treasure roll drops.
