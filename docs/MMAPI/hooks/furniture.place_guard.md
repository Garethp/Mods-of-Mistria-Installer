# Hook: furniture.place_guard

Veto a furniture placement before it is written.

`furniture.place_guard` is a **guard** hook. Register a callback with `mmapi_guard`. See [Hooks](../HOOKS.md) for how registration and dispatch work.

## Contract

Fires at the top of `write_furniture_to_location(grid, xx, yy, proto, stack_count, rotation)`, before the placement is validated or written. ctx is `{ grid, x, y, proto, stack_count, rotation }`. `proto` is the furniture `NODE_PROTOTYPE` (`proto.object_id` identifies which furniture). Return `false` to veto the placement. `write_furniture_to_location` returns `undefined`, so nothing is placed and the item stays in the player's hand. `undefined` or `true` allows.

`stack_count > 0` indicates a recursive child-grid placement (furniture on furniture). Use the guard to cap or forbid placing a mod's furniture under mod-specific conditions.

| | |
| --- | --- |
| **Fires** | At the top of `write_furniture_to_location()`, before the placement is validated or written. |
| **ctx** | `{ grid, x, y, proto, stack_count, rotation }` |
| **Kind contract** | The callback returns `false` to veto the action. `undefined` or `true` allows it. Guards fail open: a callback that throws counts as allow. |

### The ctx struct

- `grid` - the grid receiving the placement.
- `x`, `y` - the target cell (the `xx`/`yy` arguments).
- `proto` - the furniture `NODE_PROTOTYPE`, whose `proto.object_id` identifies which furniture is being placed.
- `stack_count` - `0` for a direct placement, while `> 0` means a recursive child-grid placement, i.e. this piece is being written onto another piece of furniture's grid.
- `rotation` - the placement rotation.

## Usage

```gml
// furniture.place_guard is a GUARD: return false to block it, undefined (or
// true) to allow. Guards fail OPEN - if your handler crashes, the action happens.
function feng_shui_furniture_place_guard(_ctx) {
    // _ctx is { grid, x, y, proto, stack_count, rotation }.
    //   .grid        - the grid receiving the placement.
    //   .x, .y       - the target cell.
    //   .proto       - the furniture NODE_PROTOTYPE; proto.object_id says
    //                  which furniture.
    //   .stack_count - 0 for a direct placement; > 0 is a recursive
    //                  child-grid placement (furniture on furniture).
    //   .rotation    - the placement rotation.
    if (_ctx.proto.object_id != "feng_shui/cursed_mirror") return undefined; // not ours
    if (_ctx.stack_count > 0) {
        return false; // veto - never on top of other furniture; item stays in hand
    }
    return undefined; // allow the placement
}

// inside your latched register function (see Mod Anatomy):
mmapi_guard("furniture.place_guard", feng_shui_furniture_place_guard);
```

## Engine Wiring

- Seam [`furniture_place_guard`](../seams/furniture_place_guard.md) dispatches from `gml/scripts/GameplaySystems/Data/Grid/Furniture.gml`, at the top of `write_furniture_to_location()`, ahead of the engine's own placement validation. On veto the engine runs `return undefined;`.

## See Also

- [object.interact](object.interact.md) - Give your placed furniture custom interact behavior.
- [input.take_press](input.take_press.md) - Veto any registered interaction press generically.
