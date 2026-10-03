# Hook: ui.preset_popup_layout

Resize the customization menu's preset popup frames and grid.

`ui.preset_popup_layout` is a **filter** hook. Register a callback with `mmapi_filter`. See [Hooks](../HOOKS.md) for how registration and dispatch work.

## Contract

Fires each time the customization menu generates its preset popup body, on popup open and again after a preset is added or deleted. The filtered value is the layout struct `{ frame_width: 46, frame_height: 47, columns: 4, rows: 2, spacing: 4 }`. `frame_width` and `frame_height` size each preset frame, `columns` and `rows` shape the grid, and `spacing` is the gap `centered_positions` puts between frames. ctx is the CustomizationMenu struct. Return the replacement struct, or `undefined` to keep the engine values. The seam re-reads every field defensively, so an `undefined` return (or a partial struct) keeps the engine value for each field it lacks.

| | |
| --- | --- |
| **Fires** | Each time the preset popup body is generated: popup open, preset added, preset deleted. |
| **Value** | The layout struct `{ frame_width: 46, frame_height: 47, columns: 4, rows: 2, spacing: 4 }`. |
| **ctx** | The CustomizationMenu struct. |
| **Kind contract** | The callback receives the current value and returns a replacement, or `undefined` to keep the current value. |

### The layout struct

- `frame_width` - width of each preset frame in pixels (engine value 46). Also centers Ari inside its frame.
- `frame_height` - height of each preset frame in pixels (engine value 47).
- `columns` - frames per row (engine value 4).
- `rows` - rows in the grid (engine value 2).
- `spacing` - gap between frames in pixels (engine value 4), applied on both axes.

> [!NOTE]
> Growing `columns` or `rows` grows the preset capacity. The stock 4x2 grid holds seven presets plus the button that creates a new preset. A grid smaller than the current preset count leaves the overflow presets unreachable in the popup without deleting them. The popup backplate is sized at popup open (230x164) and does not adapt, so frames that outgrow it draw outside its art.

## Usage

```gml
// ui.preset_popup_layout is a FILTER: you receive (value, ctx) and return a
// replacement, or undefined to keep the game's value.
function roomy_wardrobe_preset_popup_layout(_value, _ctx) {
    // _value is the layout struct.
    //   .frame_width  - width of each preset frame (46).
    //   .frame_height - height of each preset frame (47).
    //   .columns      - frames per row (4).
    //   .rows         - rows in the grid (2).
    //   .spacing      - gap between frames (4).
    // _ctx is the CustomizationMenu struct.
    if (_value == undefined) return undefined; // test undefined BEFORE anything else
    // _value.frame_width = 30;
    // return _value;
    return undefined; // undefined = keep the game's value
}

mmapi_filter("ui.preset_popup_layout", roomy_wardrobe_preset_popup_layout);
```

## Engine Wiring

- Seam [`ui_preset_popup_layout`](../seams/ui_preset_popup_layout.md) dispatches from `gml/scripts/UI/Anchor/Menus/CustomizationMenu.gml`, rewriting the four layout statics at the top of `generate_preset_popup_body()` into reads of the filtered struct on every call.

## See Also

- [ui.backplate_sprite](ui.backplate_sprite.md) - Swap the backplate sprites behind the mines menu and spell cards.
- [ui.button_sprites](ui.button_sprites.md) - Swap the sprite set a UI button is built from.
- [ui.menu_refreshed](ui.menu_refreshed.md) - React when a menu rebuilds its content.
