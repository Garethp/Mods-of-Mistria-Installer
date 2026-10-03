# Hook: furniture.floor_sprite

Swap a furniture piece's floor sprite as its renderer is built.

`furniture.floor_sprite` is a **filter** hook. Register a callback with `mmapi_filter`. See [Hooks](../HOOKS.md) for how registration and dispatch work.

## Contract

Fires in `create_furniture_renderer(node)` as a furniture node's floor sprite is resolved, after the engine's native `winter_floor_sprite` override and before the floor renderer is created from it. The filtered value is the floor sprite. ctx is the furniture node. Return the replacement sprite, or `undefined` to keep the current value.

| | |
| --- | --- |
| **Fires** | In `create_furniture_renderer(node)`, after the native `winter_floor_sprite` override, before the floor renderer is created from the sprite. |
| **Value** | The floor sprite about to be assigned to the furniture's floor renderer. |
| **ctx** | The furniture node. |
| **Kind contract** | The callback receives the current value and returns a replacement, or `undefined` to keep the current value. |

### The ctx parameter

ctx is the furniture node whose renderer is being built. Read `ctx.object_id` (which furniture), `ctx.prototype`, and `ctx.cardinal_index` (rotation) to decide whether this piece is one you skin.

> [!IMPORTANT]
> This hook fires once per furniture render **build**, at placement and on every room/grid load, and not per frame. A changed decision (a new season, different weather) applies the next time the node's renderer is rebuilt (re-enter the room, or pick the piece up and place it again), not instantly on a live world.

> [!NOTE]
> Furniture floor sprites never route through `obj_node_renderer.set_sprite`, so [object.node_sprite](object.node_sprite.md) cannot reach them. That hook covers world nodes (crops, forage, resource nodes). A mod that swaps sprites and wants both must register on both hooks. The placement preview's ghost is a third path, written straight from the prototype every frame, and [furniture.preview_sprite](furniture.preview_sprite.md) covers it. Register the same handler there so the ghost agrees with the piece.

## Usage

```gml
// furniture.floor_sprite is a FILTER: you receive (value, ctx) and return a
// replacement, or undefined to keep the game's value.
function fresh_coat_furniture_floor_sprite(_value, _ctx) {
    // _value is the floor sprite about to be assigned.
    // _ctx is the furniture node (.object_id, .prototype, .cardinal_index).
    if (_value == undefined) return undefined; // test undefined BEFORE anything else
    // if (<this floor sprite is yours>) return spr_fresh_coat_variant;
    return undefined; // undefined = keep the game's value
}

mmapi_filter("furniture.floor_sprite", fresh_coat_furniture_floor_sprite);
```

## Engine Wiring

- Seam [`furniture_floor_sprite`](../seams/furniture_floor_sprite.md) dispatches from `gml/scripts/GameplaySystems/Data/Grid/Furniture.gml`, in `create_furniture_renderer(node)`, between the `winter_floor_sprite` override and the floor renderer's creation.

## See Also

- [object.node_sprite](object.node_sprite.md) - Swap the sprite of a world node instead, such as crops, forage, and resource nodes.
- [furniture.preview_sprite](furniture.preview_sprite.md) - Swap the same sprites on the placement ghost, which resolves every frame while the item is held.
- [furniture.place_guard](furniture.place_guard.md) - Veto a furniture placement before it is written.
- [object.interact](object.interact.md) - Take over grid-object interactions.
