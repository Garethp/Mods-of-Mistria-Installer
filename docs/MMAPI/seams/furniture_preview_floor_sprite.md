# Seam: furniture_preview_floor_sprite

Filters the floor sprite the furniture placement preview is about to draw.

`furniture_preview_floor_sprite` is a **text seam**, a verbatim `anchor`/`replace` edit. It feeds [furniture.preview_sprite](../hooks/furniture.preview_sprite.md). Mod authors never write seams. You register handlers for the hooks they dispatch. See [Seams](../SEAMS.md).

## Placement

| | |
| --- | --- |
| **File** | `gml/scripts/GameplaySystems/Data/Grid/Furniture.gml` |
| **Locator** | text anchor in `create_test_placement_furniture_draw_info()`, on the `if` of three lines that writes the prototype's `floor_sprite` to `furniture_renderer.bottom_sprite` |
| **Op** | text (`anchor` + `replace`) |
| **Feeds** | [`furniture.preview_sprite`](../hooks/furniture.preview_sprite.md) |
| **Value filtered** | the floor sprite, starting as `proto.cardinal_data[calc_rot].floor_sprite` |
| **ctx built** | `{ object_id: object_id, prototype: proto, cardinal_index: calc_rot, x: xx, y: yy, source: "floor_sprite" }` |
| **Marker** | `mmapi_furniture_preview_floor_sprite` |

## The Edit

Pristine writes the prototype's floor sprite straight into the previewer's `bottom_sprite` field, with no local in between, so there is nothing for a template filter to reassign. The replace keeps the enclosing `if` and introduces a local. It reads the prototype's `floor_sprite` into `__mmapi_preview_floor`, threads that through `mmapi_apply_filters("furniture.preview_sprite", ...)` under its own try/catch, and writes the result to `bottom_sprite`. A throwing handler leaves the engine's sprite in place. With zero handlers the dispatch returns early and the write is the pristine one.

The ctx literal is the same shape as the [main sprite site](furniture_preview_sprite.md) builds, with `source: "floor_sprite"` so a handler can tell the two apart. The seam deliberately applies no `winter_floor_sprite` override of its own. Pristine applies none at this site either, unlike `create_furniture_renderer()`, so the value handed to handlers is exactly the sprite the engine would have drawn, and a handler that wants the ghost's winter floor to match the placed piece's must return it itself.

The two preview seams touch different lines of the same function and neither re-emits the other's text, so they carry no dependency edge and apply in catalog order.

## See Also

- [furniture.preview_sprite](../hooks/furniture.preview_sprite.md) - This is the hook this seam dispatches.
- [furniture_preview_sprite](furniture_preview_sprite.md) - This is the companion site in the same function, covering the ghost's main sprite.
- [furniture_floor_sprite](furniture_floor_sprite.md) - This is the filter on the placed piece's floor sprite, which does apply the native winter override before dispatching.
