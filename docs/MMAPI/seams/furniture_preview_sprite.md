# Seam: furniture_preview_sprite

Filters the main sprite the furniture placement preview is about to draw.

`furniture_preview_sprite` is a **template seam** (`op = "filter"`). It feeds [furniture.preview_sprite](../hooks/furniture.preview_sprite.md). Mod authors never write seams. You register handlers for the hooks they dispatch. See [Seams](../SEAMS.md).

## Placement

| | |
| --- | --- |
| **File** | `gml/scripts/GameplaySystems/Data/Grid/Furniture.gml` |
| **Locator** | pristine context in `create_test_placement_furniture_draw_info()`, between the `{Season}_sprite` override and the write to `furniture_renderer.sprite_index` |
| **Op** | `filter` |
| **Feeds** | [`furniture.preview_sprite`](../hooks/furniture.preview_sprite.md) |
| **Var** | `spr` |
| **ctx built** | `{ object_id: object_id, prototype: proto, cardinal_index: calc_rot, x: xx, y: yy, source: "main_sprite" }` |
| **Marker** | `mmapi_furniture_preview_sprite` |

## The Edit

The generated dispatch lands inside `create_test_placement_furniture_draw_info()`, the function `obj_tile_cursor` calls every frame to shape the placement ghost while a placeable furniture item is held. It sits after the engine's own seasonal pick, which replaces the local `spr` with the prototype's `{Season}_sprite` when one exists, and before that local is committed to the previewer's `sprite_index`. It threads `spr` through `mmapi_apply_filters("furniture.preview_sprite", spr, ctx)` under a try/catch, so a throwing handler keeps the engine's sprite rather than aborting the preview partway through the frame.

The ctx is a struct literal rather than a node, because nothing has been placed. `object_id` and `proto` are read after the house stairs pairing swap at the top of the function, so they describe the piece the ghost actually shows. `calc_rot` is the rotation the preview resolved, and `xx` and `yy` are the anchor cell after centring. The literal `source: "main_sprite"` tells a handler which of the hook's two sites it is on.

This site runs once per frame for as long as the item is held, which is the one way it differs from the sprite seams that run when a renderer is built. With zero handlers the filter dispatch returns early on an empty registry, leaving vanilla behavior.

## See Also

- [furniture.preview_sprite](../hooks/furniture.preview_sprite.md) - This is the hook this seam dispatches.
- [furniture_preview_floor_sprite](furniture_preview_floor_sprite.md) - This is the companion site in the same function, covering the ghost's floor sprite.
- [furniture_floor_sprite](furniture_floor_sprite.md) - This is the filter on the placed piece's floor sprite that this pair keeps the ghost in step with.
- [node_renderer_set_sprite](node_renderer_set_sprite.md) - This is the filter on the placed piece's main sprite, shared with every world node.
