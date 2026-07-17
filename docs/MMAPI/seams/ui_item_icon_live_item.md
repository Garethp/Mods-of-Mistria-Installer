# Seam: ui_item_icon_live_item

Wraps `LiveItem.get_ui_icon()` so every item icon lookup is filterable.

`ui_item_icon_live_item` is a **template seam** (`op = "wrap"`). It feeds [ui.item_icon](../hooks/ui.item_icon.md). Mod authors never write seams. You register handlers for the hooks they dispatch. See [Seams](../SEAMS.md).

## Placement

| | |
| --- | --- |
| **File** | `gml/scripts/GameplaySystems/Items/LiveItem.gml` |
| **Locator** | whole-function wrap of `get_ui_icon()` |
| **Op** | `wrap` |
| **Feeds** | [`ui.item_icon`](../hooks/ui.item_icon.md) |
| **Value filtered** | the icon sprite `get_ui_icon()` returns |
| **ctx built** | `{ item: self, source: "live_item" }` |
| **Marker** | `mmapi_ui_run_item_icon_filters` |

## The Edit

A wrap targets the whole function: the pristine `get_ui_icon` definition is renamed, its body untouched, and a generated wrapper takes its place. It calls the renamed original and filters the returned sprite through `mmapi_apply_filters("ui.item_icon", <return>, { item: self, source: "live_item" })` in the uniform try/catch shape. Every engine call site routes through `get_ui_icon` (held and thrown sprites included), so this one wrap makes every icon resolved from a `LiveItem` replaceable. Handlers key off `ctx.source == "live_item"` to tell this site from the world-item dispatch.

With zero handlers a wrap is behaviorally (not byte-) equivalent to pristine: the wrapper adds a call frame and an empty-registry early-out, nothing more.

## See Also

- [ui.item_icon](../hooks/ui.item_icon.md) - This is the hook this seam dispatches.
- [ui_item_icon_obj_item_world](ui_item_icon_obj_item_world.md) - This is the world-drop dispatch site for the same hook.
- [obj_item_outline_sprite](obj_item_outline_sprite.md) - This is the companion edit that reroutes the world-item outline draw.
- [item_display_description](item_display_description.md) - This is the other `LiveItem.gml` wrap.
