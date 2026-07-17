# Seam: camera_culls_processed

Emits the end-of-cull moment so mods can refresh renderers the camera just reactivated.

`camera_culls_processed` is a **text seam** (`anchor` + `replace`). It feeds [camera.culls_processed](../hooks/camera.culls_processed.md). Mod authors never write seams. You register handlers for the hooks they dispatch. See [Seams](../SEAMS.md).

## Placement

| | |
| --- | --- |
| **File** | `gml/scripts/GameplaySystems/Camera/Camera.gml` |
| **Locator** | text anchor at the end of `process_culls()`: the multi-line `instance_activate_region(...)` call plus the closing brace |
| **Feeds** | [`camera.culls_processed`](../hooks/camera.culls_processed.md) |
| **ctx built** | `self` - the `Camera` struct |
| **Marker** | `mmapi_camera_run_culls_processed` |

## The Edit

The injected emit is the last thing `Camera.process_culls()` runs: immediately after `instance_activate_region(...)` has deactivated every node renderer (`obj_node_renderer` / `obj_assetobject`) and reactivated only the ones inside the cull region. `try { mmapi_emit("camera.culls_processed", self); } catch (...) {}` hands handlers the live `Camera` struct, once per frame culling runs for the room, before the frame draws.

This is a text seam rather than a structural `target`/`at = "after"` because the anchor is the multi-line `instance_activate_region(...)` call itself: `instance_activate_region` is unique to this function, so the call plus the closing brace anchor exactly once.

The placement is the point. A renderer that just scrolled into view is reactivated carrying its last `sprite_index`, and the mmapi begin_step tick (which runs before this cull, at `Game.gml` `step_begin`) cannot have refreshed it yet. Re-applying per-instance sprite and visual state from this hook closes that 1-frame scroll-in lag (the catalog's example: `crop_timers` re-skins its badges here), so a mod's sprite override never lags a frame on scroll-in.

## See Also

- [camera.culls_processed](../hooks/camera.culls_processed.md) - This is the hook this seam dispatches.
- [node_renderer_set_sprite](node_renderer_set_sprite.md) - This seam is the node-renderer sprite filter whose scroll-in state this hook lets you re-apply.
- [game_step_begin_installs](game_step_begin_installs.md) - This seam installs the begin_step tick that runs before this cull each frame.
