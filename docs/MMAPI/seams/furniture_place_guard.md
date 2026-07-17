# Seam: furniture_place_guard

Puts a veto check in front of every furniture placement.

`furniture_place_guard` is a **template seam** (`op = "guard"`). It feeds [furniture.place_guard](../hooks/furniture.place_guard.md). Mod authors never write seams. You register handlers for the hooks they dispatch. See [Seams](../SEAMS.md).

## Placement

| | |
| --- | --- |
| **File** | `gml/scripts/GameplaySystems/Data/Grid/Furniture.gml` |
| **Locator** | pristine context at the head of `write_furniture_to_location()`, before the first validation `if` |
| **Op** | `guard` |
| **Feeds** | [`furniture.place_guard`](../hooks/furniture.place_guard.md) |
| **ctx built** | `{ grid: grid, x: xx, y: yy, proto: proto, stack_count: stack_count, rotation: rotation }` |
| **On veto** | `return undefined;` |

## The Edit

The generated dispatch lands at the head of `write_furniture_to_location(grid, xx, yy, proto, stack_count, rotation=0)`, before the placement is validated (the pristine `cardinals`/`stack_count` check that follows) or written. It calls `mmapi_check_guards("furniture.place_guard", ...)` in the uniform try/catch shape with the full six-field ctx, which carries the target grid, the tile coordinates, `proto` (the furniture `NODE_PROTOTYPE`, whose `proto.object_id` identifies which furniture), the stack count, and the rotation. `stack_count > 0` marks a recursive child-grid placement: furniture being placed on furniture.

When any guard returns `false`, the injected line runs `return undefined;`. `write_furniture_to_location` returns `undefined`, nothing is placed, and the item stays in the player's hand. `undefined` or `true` allows. With zero handlers the guard check early-outs on an empty registry, leaving pristine behavior.

## See Also

- [furniture.place_guard](../hooks/furniture.place_guard.md) - This is the hook this seam dispatches.
- [object_interact](object_interact.md) - This seam is the override on interacting with placed grid objects.
