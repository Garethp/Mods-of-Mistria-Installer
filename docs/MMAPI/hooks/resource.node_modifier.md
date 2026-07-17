# Hook: resource.node_modifier

Change the charged-tool modifier on picks and chops.

`resource.node_modifier` is a **filter** hook. Register a callback with `mmapi_filter`. See [Hooks](../HOOKS.md) for how registration and dispatch work.

## Contract

Fires at the top of `pick_node()` and `chop_node()`, before the tool's modifier is applied to a resource node (rocks, forage, dig sites, trees, stumps). The filtered value is the tool modifier: the charged-tool bonus/penalty added to `item.damage` and to the `item.quality` gate. ctx is `{ grid, x, y, item, action }`. `action` is `"pick"` or `"chop"`. Return the replacement modifier (for example clamp a negative charged-tool penalty to `0` to remove the charged-attack damage penalty), or `undefined` to keep the current value.

| | |
| --- | --- |
| **Fires** | At the top of `pick_node()` and `chop_node()`, before the tool's modifier is applied to a resource node. |
| **Value** | The tool modifier: the charged-tool bonus/penalty added to `item.damage` and to the `item.quality` gate. |
| **ctx** | `{ grid, x, y, item, action }` |
| **Kind contract** | The callback receives the current value and returns a replacement, or `undefined` to keep the current value. |

### The ctx struct

- `grid` - the grid the node lives in.
- `x`, `y` - the node's cell coordinates (the `x_pos`/`y_pos` arguments).
- `item` - The acting item. The engine adds the filtered modifier to `item.damage` and factors it into the `item.quality` gate.
- `action` - `"pick"` or `"chop"`, naming which entry point fired.

## Usage

```gml
// resource.node_modifier is a FILTER: you receive (value, ctx) and return a
// replacement, or undefined to keep the game's value.
function gentle_swing_resource_node_modifier(_value, _ctx) {
    // _value is the tool modifier: the charged-tool bonus/penalty the engine
    // adds to item.damage and to the item.quality gate.
    // _ctx is { grid, x, y, item, action }.
    //   .grid   - the grid the node lives in.
    //   .x, .y  - the node's cell coordinates.
    //   .item   - the acting item (receives modifier on .damage / .quality gate).
    //   .action - "pick" or "chop".
    if (_value == undefined) return undefined; // test undefined BEFORE anything else
    // remove the charged-attack damage penalty on chops:
    if (_ctx.action == "chop" && _value < 0) return 0;
    return undefined; // undefined = keep the game's value
}

mmapi_filter("resource.node_modifier", gentle_swing_resource_node_modifier);
```

## Engine Wiring

- Seam [`pick_node_modifier`](../seams/pick_node_modifier.md) dispatches from `gml/scripts/GameplaySystems/Data/Grid/GridActions/Pick.gml`, at the head of `pick_node()`, with `action: "pick"`.
- Seam [`chop_node_modifier`](../seams/chop_node_modifier.md) dispatches from `gml/scripts/GameplaySystems/Data/Grid/GridActions/Chop.gml`, at the head of `chop_node()`, with `action: "chop"`.

## See Also

- [combat.tarball_grid](combat.tarball_grid.md) - Let a swing pick/chop/destroy grid nodes in the first place.
- [object.node_sprite](object.node_sprite.md) - Swap the sprite of any world node.
- [items.dig_artifact](items.dig_artifact.md) - Swap the artifact a dig spot yields.
