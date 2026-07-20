# Hook: ui.hud_should_show

Change whether the HUD shows.

`ui.hud_should_show` is a **filter** hook. Register a callback with `mmapi_filter`. See [Hooks](../HOOKS.md) for how registration and dispatch work.

## Contract

Fires at the return of `hud_should_show()`. The filtered value is the computed boolean. ctx is `undefined`. Return the replacement boolean, or `undefined` to keep the current value.

| | |
| --- | --- |
| **Fires** | At the return of `hud_should_show()`. |
| **Value** | The computed boolean. |
| **ctx** | `undefined` |
| **Kind contract** | The callback receives the current value and returns a replacement, or `undefined` to keep the current value. |

## Usage

```gml
// ui.hud_should_show is a FILTER: you receive (value, ctx) and return a
// replacement, or undefined to keep the game's value.
function screenshot_mode_ui_hud_should_show(_value, _ctx) {
    // _value is the boolean hud_should_show() just computed.
    // _ctx is undefined.
    if (_value == undefined) return undefined; // test undefined BEFORE anything else
    // force the HUD hidden while your mod's screenshot mode is active:
    // if (__screenshot_mode_active()) return false;
    return undefined; // undefined = keep the game's value
}

mmapi_filter("ui.hud_should_show", screenshot_mode_ui_hud_should_show);
```

## Engine Wiring

- Seam [`ui_hud_should_show`](../seams/ui_hud_should_show.md) dispatches from `gml/scripts/UI/Anchor/anchor_utils.gml`, a whole-function wrap of `hud_should_show()` that filters its return value.

## See Also

- [ui.draw_gui](ui.draw_gui.md) - React to every GUI draw with your own overlay.
- [ui.menu_opened](ui.menu_opened.md) - Know the moment a menu opens.
- [ui.toolbar_tick](ui.toolbar_tick.md) - React on every toolbar tick.
