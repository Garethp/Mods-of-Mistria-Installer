# Hook: object.node_sprite

Swap the sprite of any world node before it draws.

`object.node_sprite` is a **filter** hook. Register a callback with `mmapi_filter`. See [Hooks](../HOOKS.md) for how registration and dispatch work.

## Contract

Fires at the top of `obj_node_renderer.set_sprite(spr)`, before the sprite is assigned, for every world node the renderer draws (crops, forage, resource nodes). The filtered value is the sprite. ctx is the `obj_node_renderer` instance. Read `ctx.node` for the node's `prototype`, `day_count`, and so on. Return the replacement sprite, or `undefined` to keep the current value.

| | |
| --- | --- |
| **Fires** | At the top of `obj_node_renderer.set_sprite(spr)`, before the sprite is assigned. |
| **Value** | The sprite about to be assigned to the renderer. |
| **ctx** | The `obj_node_renderer` instance. |
| **Kind contract** | The callback receives the current value and returns a replacement, or `undefined` to keep the current value. |

### The ctx parameter

ctx is the `obj_node_renderer` instance about to receive the sprite. Read `ctx.node` for the node data (`ctx.node.prototype`, `ctx.node.day_count`, and so on) to decide whether this node is one you skin.

> [!NOTE]
> A renderer that scrolls off-screen is deactivated by the camera cull and carries its last `sprite_index` when it reactivates. Observe [camera.culls_processed](camera.culls_processed.md) to re-apply your sprite to just-reactivated renderers before they draw, so your override does not lag a frame on scroll-in.

## Usage

```gml
// object.node_sprite is a FILTER: you receive (value, ctx) and return a
// replacement, or undefined to keep the game's value.
function fresh_coat_object_node_sprite(_value, _ctx) {
    // _value is the sprite about to be assigned to the renderer.
    // _ctx is the obj_node_renderer instance.
    //   .node - the node being drawn; read .node.prototype and
    //           .node.day_count to decide if this node is yours.
    if (_value == undefined) return undefined; // test undefined BEFORE anything else
    // if (<this node is yours>) return spr_fresh_coat_variant;
    return undefined; // undefined = keep the game's value
}

mmapi_filter("object.node_sprite", fresh_coat_object_node_sprite);
```

## Engine Wiring

- Seam [`node_renderer_set_sprite`](../seams/node_renderer_set_sprite.md) dispatches from `gml/objects/objects/obj_node_renderer.gml`, in `set_sprite(spr)`, just before `self.sprite_index = spr;`.

## See Also

- [camera.culls_processed](camera.culls_processed.md) - Re-apply sprite state to just-reactivated renderers.
- [ui.item_icon](ui.item_icon.md) - Swap item icon sprites in the UI and world.
- [object.interact](object.interact.md) - Take over grid-object interactions.
- [resource.node_modifier](resource.node_modifier.md) - Change the charged-tool modifier on picks and chops.
