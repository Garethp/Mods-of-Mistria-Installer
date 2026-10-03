# Engine Fix: pet_appearance_popup_scrollable

Wraps the pet "Select an Appearance" variant grid in the same capped-height scroller when it exceeds 7 rows, so pet-skin mods that add many variants stay on-screen.

`pet_appearance_popup_scrollable` is an **engine fix**. See [Seams](../SEAMS.md).

## Placement

| | |
| --- | --- |
| **File** | `gml/scripts/AnimalMenu.gml` |
| **Locator** | text anchor: the `GridLayout(ListFromArray(order), popup.pilot, 10)` container/height block in `spawn_pet_appearance_popup()` |
| **Feeds** | (no hook) |
| **Marker** | `mmapi_pet_appearance_scrollable` |

## The Edit

`spawn_pet_appearance_popup` lists every pet variant (`fiddle_get("ui/misc/pet_variant_order")`) as a 10-column `GridLayout` and then grows the popup backplate to fit the whole grid (`popup.backplate.add_height(container_size.y)`) with no scroll. The vanilla variant list is short, but pet-skin mods that register many new variants pushing the grid past the screen and making the extra rows become unreachable.


## See Also

- [customization_color_popup_scrollable](customization_color_popup_scrollable.md) - Uses the, practically, same fix.
