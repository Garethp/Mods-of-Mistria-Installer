# Hook: camera.culls_processed

React the instant culling reactivates on-screen renderers, before they draw.

`camera.culls_processed` is an **event** hook. Register a callback with `mmapi_on`. See [Hooks](../HOOKS.md) for how registration and dispatch work.

## Contract

Fires at the end of `Camera.process_culls()`, immediately after `instance_activate_region()` has deactivated all node renderers (`obj_node_renderer` / `obj_assetobject`) and reactivated only the ones inside the cull region, i.e. after off-screen instances that just scrolled into view are active again and before the frame draws. ctx is the `Camera` struct. Fires every frame culling runs for the room. This hook is observation only.

The intended use is to re-apply per-instance sprite/visual state to the just-reactivated renderers before they draw, so a mod's sprite override does not lag a frame on scroll-in. A reactivated instance carries its last `sprite_index`, which the mmapi begin_step tick (running before this cull, at `Game.gml` step_begin) cannot have refreshed yet.

Plainly: if your mod changes what world renderers look like, a renderer that scrolls into view was deactivated while you changed things, so it wakes up wearing whatever sprite it had when it scrolled out. Your begin_step code cannot fix it. Deactivated instances are invisible to it, and this frame's cull happens after begin_step already ran. This hook is the one moment the instance is active again but has not drawn yet: sweep the just-reactivated renderers and re-apply your sprite state here, and the override is correct on the very first visible frame.

| | |
| --- | --- |
| **Fires** | At the end of `Camera.process_culls()`, right after `instance_activate_region()` reactivates the in-region renderers and before the frame draws. |
| **ctx** | The `Camera` struct. |
| **Kind contract** | The callback observes the moment. Its return value is ignored. |

### The ctx parameter

- ctx - the engine's `Camera` struct itself, the object whose `process_culls()` just ran.

> [!IMPORTANT]
> Hot path. This fires every frame culling runs for the room. Make the callback's first check its cheapest early-exit, and touch only the instances your mod actually re-skins.

## Usage

```gml
// camera.culls_processed is an EVENT: mmapi calls you after it happens.
// You cannot change or stop it here; the return value is ignored.
function crop_timers_camera_culls_processed(_ctx) {
    // _ctx is the Camera struct whose process_culls() just ran.
    // HOT PATH: fires every frame culling runs for the room. Make your first
    // check the cheapest one and get out early when your mod has nothing to do.
    if (!__crop_timers_runtime().has_overrides) return;
    // Re-apply your per-instance sprite state to the now-active renderers
    // so instances that just scrolled into view draw correctly this frame:
    // with (obj_node_renderer) {
    //     if (<this renderer is one of yours>) sprite_index = <your sprite>;
    // }
}

// inside your latched register function (see Mod Anatomy):
mmapi_on("camera.culls_processed", crop_timers_camera_culls_processed);
```

## Engine Wiring

- Seam [`camera_culls_processed`](../seams/camera_culls_processed.md) dispatches from `gml/scripts/GameplaySystems/Camera/Camera.gml`, an emit anchored on the multi-line `instance_activate_region(...)` call at the end of `process_culls()`.

## See Also

- [object.node_sprite](object.node_sprite.md) - This is the sprite filter for world node renderers, and this hook keeps their overrides from lagging on scroll-in.
- [game.room_changed](game.room_changed.md) - This hook is the coarser signal for re-applying state when the whole room changes.
