# Seam: item_display_description

Wraps the item-description getter, the string the tooltip body actually renders.

`item_display_description` is a **template seam** (`op = "wrap"`). It feeds [item.display_description](../hooks/item.display_description.md). Mod authors never write seams. You register handlers for the hooks they dispatch. See [Seams](../SEAMS.md).

## Placement

| | |
| --- | --- |
| **File** | `gml/scripts/GameplaySystems/Items/LiveItem.gml` |
| **Locator** | whole-function wrap of `get_display_description()` |
| **Op** | `wrap` |
| **Feeds** | [`item.display_description`](../hooks/item.display_description.md) |
| **Value filtered** | the description string `get_display_description()` returns |
| **ctx built** | `{ item: self }` |
| **Marker** | `mmapi_item_display_description` |

## The Edit

This is the rendered item-description chokepoint. `TooltipMenu` (and `CraftingMenu`) build the tooltip body from `item.get_display_description()`, while the `ui.item_node` seams only reach the icon node, whose `.description` field is never drawn. Wrapping the method funnels everything to a single filtered return: the pristine `get_display_description` is renamed with its body untouched, and the generated wrapper calls it and filters the returned string through `mmapi_apply_filters("item.display_description", <return>, { item: self })` in the uniform try/catch shape. A synthesized addition (the set-bonus `[n/5]` line, say) lands on the string the tooltip actually renders. This is the faithful chokepoint for that string.

With zero handlers a wrap is behaviorally (not byte-) equivalent to pristine: one extra call frame and an empty-registry early-out.

## See Also

- [item.display_description](../hooks/item.display_description.md) - This is the hook this seam dispatches.
- [ui_item_icon_live_item](ui_item_icon_live_item.md) - This is the other `LiveItem.gml` wrap, for the icon sprite.
- [ui_item_node_set_to_item](ui_item_node_set_to_item.md) - Restyle the item node itself, whose `.description` is never drawn.
