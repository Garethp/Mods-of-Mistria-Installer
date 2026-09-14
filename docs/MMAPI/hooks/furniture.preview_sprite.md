# Hook: furniture.preview_sprite

Swap the sprites the furniture placement preview draws, so the ghost agrees with the piece.

`furniture.preview_sprite` is a **filter** hook. Register a callback with `mmapi_filter`. See [Hooks](../HOOKS.md) for how registration and dispatch work.

## Contract

Fires in `create_test_placement_furniture_draw_info()` as the furniture placement preview resolves each sprite it will draw. The preview is the translucent ghost (`obj_furniture_previewer`) shown under the cursor while a placeable furniture item is held. `ctx.source` names the dispatch site: `main_sprite` (the piece's main sprite, after the engine's own `{Season}_sprite` override) and `floor_sprite` (the floor sprite, which the preview reads raw, because the engine applies no `winter_floor_sprite` override here). The filtered value is that sprite. ctx is `{ object_id, prototype, cardinal_index, x, y, source }`. There is no node, because nothing is placed yet. Return the replacement sprite, or `undefined` to keep the current value.

| | |
| --- | --- |
| **Fires** | In `create_test_placement_furniture_draw_info()`, once per site the previewed piece has, every frame a placeable furniture item is held. |
| **Value** | The sprite the preview is about to draw at that site. |
| **ctx** | `{ object_id, prototype, cardinal_index, x, y, source }` |
| **Kind contract** | The callback receives the current value and returns a replacement, or `undefined` to keep the current value. |

### The ctx struct

- `object_id` - the `ObjectId` of the piece being previewed. When a house stairs piece cannot be placed in the current location the engine previews its paired object instead, and this is the paired id.
- `prototype` - that piece's `NODE_PROTOTYPES` entry, the same struct a placed node carries as `node.prototype`.
- `cardinal_index` - the resolved rotation, the index into `prototype.cardinal_data` the preview draws from.
- `x`, `y` - the centred anchor cell, in grid cells.
- `source` - names the dispatch site: `"main_sprite"` (the main sprite) or `"floor_sprite"` (the floor sprite).

> [!IMPORTANT]
> This hook fires **every frame** while a placeable is held, not once per build the way [furniture.floor_sprite](furniture.floor_sprite.md) does. Keep handlers cheap. Look the incoming value up first and return `undefined` at once when it is not a sprite you own, and only then read the calendar, weather, or anything else that costs.

> [!NOTE]
> The preview draws the piece's `top_sprite` and `secondary_sprite` too, but this hook does not filter them. Neither is reachable for a *placed* piece by any hook either, so filtering them only in the preview would make the ghost disagree with the piece the other way around. A future hook covering both sites can add them together.

## Why the ghost needs its own hook

The preview never touches a node renderer. Every frame, `obj_tile_cursor` asks `create_test_placement_furniture_draw_info()` to write the ghost's sprites straight from the prototype into `obj_furniture_previewer`, which draws them and clears them again. Nothing in that path calls `obj_node_renderer.set_sprite()` or `create_furniture_renderer()`, so neither [object.node_sprite](object.node_sprite.md) nor [furniture.floor_sprite](furniture.floor_sprite.md) can reach it. A mod that swaps a placed piece's sprites through those hooks therefore shows the player a ghost that does not match what they are about to place. Registering the same handler here closes that gap. The ghost and the placed piece read their sprites from the same prototype fields, so the asset a handler receives is identical on every hook. A handler that decides by that asset alone works on all three without a branch per hook.

## Usage

```gml
// furniture.preview_sprite is a FILTER: you receive (value, ctx) and return a
// replacement, or undefined to keep the game's value.
function autumn_stump_furniture_preview_sprite(_value, _ctx) {
    // _value is the sprite the placement ghost is about to draw.
    // _ctx is { object_id, prototype, cardinal_index, x, y, source }.
    //   .source - "main_sprite" or "floor_sprite", the site being resolved.
    if (_value == undefined) return undefined; // test undefined BEFORE anything else
    // This fires every frame while a placeable is held: decide on the sprite
    // first, and only read the calendar once you know the piece is yours.
    if (_value != spr_autumn_stump_floor_spring) return undefined;
    if (CALENDAR.season() == Season.Fall) return spr_autumn_stump_floor_autumn;
    return undefined; // undefined = keep the game's value
}

// The same handler on the hooks that cover the PLACED piece keeps the ghost
// and the piece in step.
mmapi_filter("furniture.preview_sprite", autumn_stump_furniture_preview_sprite);
mmapi_filter("furniture.floor_sprite", autumn_stump_furniture_preview_sprite);
```

## Engine Wiring

- Seam [`furniture_preview_sprite`](../seams/furniture_preview_sprite.md) dispatches from `gml/scripts/GameplaySystems/Data/Grid/Furniture.gml`, in `create_test_placement_furniture_draw_info()`, after the engine's seasonal override of the main sprite and before the sprite is written to the previewer (`source: "main_sprite"`).
- Seam [`furniture_preview_floor_sprite`](../seams/furniture_preview_floor_sprite.md) dispatches from the same function, where the floor sprite is written to the previewer's `bottom_sprite` (`source: "floor_sprite"`).

## See Also

- [furniture.floor_sprite](furniture.floor_sprite.md) - Swap the placed piece's floor sprite, which resolves once per renderer build.
- [object.node_sprite](object.node_sprite.md) - Swap the placed piece's main sprite, along with every other world node's.
- [furniture.place_guard](furniture.place_guard.md) - Veto the placement the preview leads to.
