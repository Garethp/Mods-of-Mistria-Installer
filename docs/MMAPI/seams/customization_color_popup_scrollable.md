# Engine Fix: customization_color_popup_scrollable

Wraps the customization colour popup's swatch grid in a capped-height scroller when it exceeds 7 rows, so LUTs widened past the vanilla colour count stay on-screen.

`customization_color_popup_scrollable` is an **engine fix**, an anchored edit with no hook behind it. Nothing dispatches. See [Seams](../SEAMS.md).

## Placement

| | |
| --- | --- |
| **File** | `gml/scripts/UI/Anchor/Menus/CustomizationMenu.gml` |
| **Locator** | text anchor on the container and height block at the tail of `create_color_popup(par_ui_slot, asset_key)` |
| **Feeds** | (no hook) |
| **Marker** | `mmapi_lut_selector_scrollable` |

## The Edit

The anchor is the block that places the swatch grid and grows the popup to fit it:

```gml
        var container_size = layout.container_size();
        var container = ANCHOR.positional(popup.backplate)
            .set_size(container_size)
            .set_align(Align.Center, Align.BottomIn)
            .set_y(-13)

        popup.backplate.add_height(container_size.y);
```

`create_color_popup` lists a cosmetic's colours as one swatch per LUT column, with `color_count = sprite_get_width(asset_data.lut_sprite)`, lays them out with `GridLayout`, and grows the popup backplate by the grid's full height with no scroll. The vanilla LUTs are narrow, so the grid never overflows. A cosmetic mod can widen a LUT far past the vanilla colour count, and then the lower swatches land off-screen where they cannot be selected.

The replace keeps the pristine layout up to seven rows of swatches. Past that it caps the popup at seven rows, widens the backplate for a scrollbar, and places the grid inside a scroller built with the engine's own `create_scroller_ext`, driven by the popup's own controls.

A grid of seven rows or fewer takes the pristine branch unchanged, and every vanilla LUT fits within it, so an intact install is unchanged.

## See Also

- [pet_appearance_popup_scrollable](pet_appearance_popup_scrollable.md) - The same scroller around the pet appearance grid.
- [Engine Fixes](../CATALOG.md#engine-fixes-and-the-call-rewrite) - The rest of the catalog's hook-free edits.
