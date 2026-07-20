# Seam: node_renderer_set_sprite

Filters the sprite every world node renderer is about to wear.

`node_renderer_set_sprite` is a **template seam** (`op = "filter"`). It feeds [object.node_sprite](../hooks/object.node_sprite.md). Mod authors never write seams. You register handlers for the hooks they dispatch. See [Seams](../SEAMS.md).

## Placement

| | |
| --- | --- |
| **File** | `gml/objects/objects/obj_node_renderer.gml` |
| **Locator** | pristine context at the head of `set_sprite(spr)`, before `self.sprite_index = spr;` |
| **Op** | `filter` |
| **Feeds** | [`object.node_sprite`](../hooks/object.node_sprite.md) |
| **Value filtered** | `spr` - the sprite about to be assigned |
| **ctx built** | `self` (the `obj_node_renderer` instance) |
| **Marker** | `mmapi_node_renderer_run_sprite_filters` |

## The Edit

The generated dispatch lands between `function set_sprite(spr) {` and `self.sprite_index = spr;`, reassigning `spr` through `mmapi_apply_filters("object.node_sprite", spr, self)` before the assignment happens. Every world node the renderer draws (crops, forage, resource nodes) resolves its sprite through `set_sprite`, so this one edit makes them all filterable. ctx is the renderer instance itself. A handler reads `ctx.node` for the node's prototype, `day_count`, and so on.

This seam sets `try_catch = false`: uniquely, the dispatch is a direct call with no try/catch wrapper of its own. The registry's per-handler isolation still applies, so a throwing handler is contained the same way as everywhere else. The seam just skips the extra wrapper around the dispatch call. With zero handlers the filter hands `spr` back unchanged.

Note that a renderer reactivated by camera culling carries its last `sprite_index`. A mod re-skinning nodes here should also watch [camera_culls_processed](camera_culls_processed.md)'s hook to re-apply state to just-reactivated renderers before they draw.

## See Also

- [object.node_sprite](../hooks/object.node_sprite.md) - This is the hook this seam dispatches.
- [camera_culls_processed](camera_culls_processed.md) - This is the post-cull event that lets sprite overrides survive scroll-in reactivation.
- [pick_node_modifier](pick_node_modifier.md) - This seam is the resource-node tool-modifier filter on pick.
