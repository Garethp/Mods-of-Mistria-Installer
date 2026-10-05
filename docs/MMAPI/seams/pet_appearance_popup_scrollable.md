# Engine Fix: pet_appearance_popup_scrollable

Wraps the pet "Select an Appearance" variant grid in the same capped-height scroller when it exceeds 7 rows, so pet-skin mods that add many variants stay on-screen.

`pet_appearance_popup_scrollable` is an **engine fix**, an anchored edit with no hook behind it. Nothing dispatches. See [Seams](../SEAMS.md).

## Placement

| | |
| --- | --- |
| **File** | `gml/scripts/AnimalMenu.gml` |
| **Locator** | text anchor on the `GridLayout(ListFromArray(order), popup.pilot, 10)` container and height block in `spawn_pet_appearance_popup()` |
| **Feeds** | (no hook) |
| **Marker** | `mmapi_pet_appearance_scrollable` |

## The Edit

The anchor is the block that lays out the variant grid and grows the popup to fit it:

```gml
    var layout = new GridLayout(ListFromArray(order), popup.pilot, 10);

    var container_size = layout.container_size();
    var container = ANCHOR.positional(popup.backplate)
        .set_size(container_size)
        .set_align(Align.Center, Align.Middle)
        .set_y(9)

    popup.backplate.add_height(container_size.y);
```

`spawn_pet_appearance_popup` lists every pet variant from `fiddle_get("ui/misc/pet_variant_order")` as a ten-column `GridLayout` and grows the popup backplate by the grid's full height with no scroll. The vanilla variant list is short, so the grid never overflows. A pet-skin mod that registers many variants pushes the grid past the screen, and the extra rows cannot be reached.

The replace is the colour popup's fix applied here. Up to seven rows the pristine layout stands. Past that the popup is capped at seven rows, the backplate widens for a scrollbar, and the grid moves into a scroller built with the engine's own `create_scroller_ext`, driven by the popup's own controls.

A grid of seven rows or fewer takes the pristine branch unchanged, and the vanilla variant list fits within it, so an intact install is unchanged.

## See Also

- [customization_color_popup_scrollable](customization_color_popup_scrollable.md) - The same scroller around the customization colour grid.
- [store_pet_cosmetic_entry](store_pet_cosmetic_entry.md) - The catalog's other edit on behalf of pet mods, in the store stock.
