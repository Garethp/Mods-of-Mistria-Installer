# Seam: furniture_floor_sprite

Filters the floor sprite as a furniture renderer is built.

`furniture_floor_sprite` is a **template seam** (`op = "filter"`). It feeds [furniture.floor_sprite](../hooks/furniture.floor_sprite.md). Mod authors never write seams. You register handlers for the hooks they dispatch. See [Seams](../SEAMS.md).

## Placement

| | |
| --- | --- |
| **File** | `gml/scripts/GameplaySystems/Data/Grid/Furniture.gml` |
| **Locator** | pristine context in `create_furniture_renderer(node)`, between the `winter_floor_sprite` override and the floor renderer's `instance_create_layer` |
| **Op** | `filter` |
| **Feeds** | [`furniture.floor_sprite`](../hooks/furniture.floor_sprite.md) |
| **Var** | `sprite_to_use` |
| **ctx built** | `node` (the furniture node) |
| **Marker** | `mmapi_furniture_floor_sprite` |

## The Edit

The generated dispatch lands inside `create_furniture_renderer(node)`'s `if cardinal_data.floor_sprite != undefined` block, after the engine's own seasonal pick (`winter_floor_sprite` in winter, the base `floor_sprite` otherwise) and before that pick is committed to the freshly created floor renderer's `sprite_index`. It threads `sprite_to_use` through `mmapi_apply_filters("furniture.floor_sprite", sprite_to_use, node)` under a try/catch, so a throwing handler keeps the engine's sprite (fail-open) rather than aborting the renderer build mid-way.

`create_furniture_renderer` runs when a furniture node's renderer is built at placement and on room/grid load. It does not run per frame, so a filter's changed decision applies on the node's next build. The nearly identical `top_sprite` block just above is intentionally not seamed. The placement preview's ghost, which reads the floor sprite raw and skips even the native winter override, has its own pair of seams in the same file. See: [furniture_preview_sprite](furniture_preview_sprite.md) and [furniture_preview_floor_sprite](furniture_preview_floor_sprite.md).

With zero handlers the filter dispatch early-outs on an empty registry, leaving pristine behavior.

## See Also

- [furniture.floor_sprite](../hooks/furniture.floor_sprite.md) - This is the hook this seam dispatches.
- [node_renderer_set_sprite](node_renderer_set_sprite.md) - This is the sprite filter for world nodes that this seam complements.
- [furniture_preview_floor_sprite](furniture_preview_floor_sprite.md) - This is the filter on the placement ghost's floor sprite, where this seam's swap should be mirrored.
- [furniture_place_guard](furniture_place_guard.md) - This is the placement veto in the same engine file.
