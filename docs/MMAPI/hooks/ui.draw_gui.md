# Hook: ui.draw_gui

React to every GUI draw with your own overlay.

`ui.draw_gui` is an **event** hook. Register a callback with `mmapi_on`. See [Hooks](../HOOKS.md) for how registration and dispatch work.

## Contract

Fires on every GUI draw, right after `ANCHOR.on_draw_gui()`. ctx is the display's `asset_resize()` value, the same argument `on_draw_gui` received.

The callback runs inside the same GUI draw pass, after the anchor has drawn the game's UI, so anything you draw lands on top of it.

| | |
| --- | --- |
| **Fires** | On every GUI draw, right after `ANCHOR.on_draw_gui()`. |
| **ctx** | The display's `asset_resize()` value. |
| **Kind contract** | The callback observes the moment. Its return value is ignored. |

### The ctx parameter

- The display's `asset_resize()` value, the same argument `ANCHOR.on_draw_gui()` just received.

## Usage

```gml
// ui.draw_gui is an EVENT: mmapi calls you after it happens.
// You cannot change or stop it here; the return value is ignored.
function hud_painter_ui_draw_gui(_ctx) {
    // _ctx is the display's asset_resize() value - the same argument
    // ANCHOR.on_draw_gui() just received.
    // You are inside the GUI draw pass, after the game's UI has drawn:
    // draw your overlay here and it lands on top.
}

// inside your latched register function (see Mod Anatomy):
mmapi_on("ui.draw_gui", hud_painter_ui_draw_gui);
```

## Engine Wiring

- Seam [`ui_draw_gui`](../seams/ui_draw_gui.md) dispatches from `gml/scripts/GameplaySystems/Camera/Display.gml`, right after the `ANCHOR.on_draw_gui(self.asset_resize())` call.

## See Also

- [ui.hud_should_show](ui.hud_should_show.md) - Change whether the HUD shows.
- [ui.toolbar_tick](ui.toolbar_tick.md) - React on every toolbar tick.
- [ui.menu_opened](ui.menu_opened.md) - Know the moment a menu opens.
