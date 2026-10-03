# Seam: ui_preset_popup_layout

Rebuilds the preset popup's layout constants through the `ui.preset_popup_layout` filter each time the popup body is generated.

`ui_preset_popup_layout` is a **text seam**, a verbatim `anchor`/`replace` edit. It feeds [ui.preset_popup_layout](../hooks/ui.preset_popup_layout.md). Mod authors never write seams. You register handlers for the hooks they dispatch. See [Seams](../SEAMS.md).

## Placement

| | |
| --- | --- |
| **File** | `gml/scripts/UI/Anchor/Menus/CustomizationMenu.gml` |
| **Locator** | text anchor on the four layout statics at the top of `generate_preset_popup_body()` |
| **Feeds** | [`ui.preset_popup_layout`](../hooks/ui.preset_popup_layout.md) |
| **Value filtered** | the struct `{ frame_width, frame_height, columns, rows, spacing }` |
| **ctx built** | `self`, the CustomizationMenu struct |
| **Marker** | `mmapi_ui_run_preset_popup_layout_filters` |

## The Edit

Pristine, the function opens with four `static` locals: `FRAME_WIDTH = 46`, `FRAME_HEIGHT = 47`, and the `HORZ_LAYOUT`/`VERT_LAYOUT` position arrays computed from them by `centered_positions(4, FRAME_WIDTH, 4)` and `centered_positions(2, FRAME_HEIGHT, 4)`. The replace builds `__mmapi_preset_layout = { frame_width: 46, frame_height: 47, columns: 4, rows: 2, spacing: 4 }`, filters it through `mmapi_apply_filters("ui.preset_popup_layout", __mmapi_preset_layout, self)` in a try/catch, and demotes the statics to plain `var` locals read back from the filtered struct. Each field is read back in its own try/catch, so an `undefined` return keeps every engine value, and a partial struct keeps the engine value for exactly the fields it lacks. The layout arrays are then rebuilt as `centered_positions(COLUMNS, FRAME_WIDTH, SPACING)` and `centered_positions(ROWS, FRAME_HEIGHT, SPACING)`.

The static demotion is the point of the edit, not a side effect. Fabricator runs a static initializer once, the first time the statement executes, so a filter left inside the initializer would freeze its first answer for the whole session and would ignore any handler whose registration lost that race. As plain locals the filter applies on every rebuild instead: popup open, preset added, preset deleted. With zero handlers the demotion is behaviorally invisible. `centered_positions` is a pure computation, so recomputing it per rebuild yields exactly the values the statics held, and the function only runs on those three edges.

Downstream, the rest of the function follows along without further edits because the local names match the pristine statics. The frame slices take `FRAME_WIDTH`/`FRAME_HEIGHT`, the grid loops measure the rebuilt layout arrays, the ari doll centers itself from the same values, and the trash callback recomputes rows and columns from the pilot map rather than the statics, so a resized grid stays navigable.

## See Also

- [ui.preset_popup_layout](../hooks/ui.preset_popup_layout.md) - This is the hook this seam dispatches.
- [ui_backplate_sprite_mines](ui_backplate_sprite_mines.md) - This is a sibling menu reskin seam, for the mines menu backplate.
- [dialogue_path](dialogue_path.md) - This seam is the model for the struct filter with defensive field reads.
