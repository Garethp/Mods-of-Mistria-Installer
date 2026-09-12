# Engine Fix: customization_color_popup_scrollable

Wraps the customization colour popup's swatch grid in a capped-height scroller when it exceeds 7 rows, so LUTs widened past the vanilla colour count stay on-screen.

`customization_color_popup_scrollable` is an **engine fix**. See [Seams](../SEAMS.md).

## Placement

| | |
| --- | --- |
| **File** | `gml/scripts/UI/Anchor/Menus/CustomizationMenu.gml` |
| **Locator** | text anchor: the container/height block at the tail of `create_color_popup(par_ui_slot, asset_key)` |
| **Feeds** | (no hook) |
| **Marker** | `mmapi_lut_selector_scrollable` |

## The Edit

`create_color_popup` lists a cosmetic's colours as one swatch per LUT column (`color_count = sprite_get_width(asset_data.lut_sprite)`), lays them out with `GridLayout`, and then grows the popup backplate to fit the whole grid (`popup.backplate.add_height(container_size.y)`) with no scroll. The vanilla LUTs are narrow, so the grid never overflows. If a large enough LUT exists some swatches are no longer selectable which this fixes.

