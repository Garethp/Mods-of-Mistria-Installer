# Hook: ui.item_icon

Swap the sprite an item shows as its icon.

`ui.item_icon` is a **filter** hook. Register a callback with `mmapi_filter`. See [Hooks](../HOOKS.md) for how registration and dispatch work.

## Contract

Fires wherever an item icon sprite is resolved. `ctx.source` names the dispatch site: `live_item` (the generated wrapper around `LiveItem.get_ui_icon`, which every engine call site routes through, held and thrown sprites included) and `obj_item_world` (world item draw, where the value starts as `get_world_icon()` and may be `undefined`). The companion outline seam provides no dispatch of its own. The filtered value is the icon sprite. ctx is `{ item, source }`. Return the replacement sprite, or `undefined` to keep the current value.

| | |
| --- | --- |
| **Fires** | Wherever an item icon sprite is resolved: the `LiveItem.get_ui_icon()` wrapper and the world item draw in `obj_item`. |
| **Value** | The icon sprite. At the `obj_item_world` site it starts as `get_world_icon()` and may be `undefined`. |
| **ctx** | `{ item, source }` |
| **Kind contract** | The callback receives the current value and returns a replacement, or `undefined` to keep the current value. |

### The ctx struct

- `item` - the `LiveItem` whose icon is being resolved: the item itself at the `live_item` site, the world item's item data at the `obj_item_world` site.
- `source` - names the dispatch site: `"live_item"` (the `get_ui_icon` wrapper every engine call site routes through, held and thrown sprites included) or `"obj_item_world"` (the world item draw).

> [!NOTE]
> At the `obj_item_world` site the incoming value may be `undefined` (no world icon). Do not use the blanket `_value == undefined` early return here, branch on `_ctx.source` and handle the `undefined` start knowingly.

## Usage

```gml
// ui.item_icon is a FILTER: you receive (value, ctx) and return a
// replacement, or undefined to keep the game's value.
function seasonal_icons_ui_item_icon(_value, _ctx) {
    // _value is the icon sprite. At the "obj_item_world" site it starts as
    // get_world_icon() and may be undefined (no world icon) - undefined is
    // meaningful here, so do not blanket early-return on it.
    // _ctx is { item, source }.
    //   .item   - the LiveItem whose icon is being resolved.
    //   .source - "live_item" (the LiveItem.get_ui_icon wrapper: every UI
    //             icon, held and thrown sprites included) or
    //             "obj_item_world" (the world item draw).
    // if (<this is your item>) {
    //     return spr_seasonal_icons_winter_variant; // both sites accept it
    // }
    return undefined; // undefined = keep the game's value
}

mmapi_filter("ui.item_icon", seasonal_icons_ui_item_icon);
```

## Engine Wiring

- Seam [`ui_item_icon_live_item`](../seams/ui_item_icon_live_item.md) dispatches from `gml/scripts/GameplaySystems/Items/LiveItem.gml`, a whole-function wrap of `get_ui_icon()` that filters its return value (`source: "live_item"`).
- Seam [`ui_item_icon_obj_item_world`](../seams/ui_item_icon_obj_item_world.md) dispatches from `gml/objects/objects/obj_item.gml`, in the world item draw: it filters `get_world_icon()` (`source: "obj_item_world"`) and re-derives the sprite metrics when the sprite changes.
- Companion seam [`obj_item_outline_sprite`](../seams/obj_item_outline_sprite.md) provides no dispatch of its own: it reroutes the world outline draw to the `outline_sprite` the world seam computes (`get_ui_outline()`, falling back to the prototype outline).

## See Also

- [ui.item_node](ui.item_node.md) - Adjust UI item slots as they are populated.
- [object.node_sprite](object.node_sprite.md) - Swap the sprite a world node renders with.
- [item.display_description](item.display_description.md) - Reword the tooltip description an item renders.
