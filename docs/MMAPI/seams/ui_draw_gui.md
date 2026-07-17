# Seam: ui_draw_gui

Emits on every GUI draw, right after the anchor draws the UI.

`ui_draw_gui` is a **template seam** (`op = "emit"`). It feeds [ui.draw_gui](../hooks/ui.draw_gui.md). Mod authors never write seams. You register handlers for the hooks they dispatch. See [Seams](../SEAMS.md).

## Placement

| | |
| --- | --- |
| **File** | `gml/scripts/GameplaySystems/Camera/Display.gml` |
| **Locator** | pristine context after the `ANCHOR.on_draw_gui(self.asset_resize())` block in the display's GUI draw |
| **Op** | `emit` |
| **Feeds** | [`ui.draw_gui`](../hooks/ui.draw_gui.md) |
| **ctx built** | `self.asset_resize()` - the same argument `on_draw_gui` received |
| **Marker** | `mmapi_ui_run_draw_gui_callbacks` |

## The Edit

The generated emit lands right after the block that calls `ANCHOR.on_draw_gui(self.asset_resize())` (the engine's whole-UI draw) and runs `mmapi_emit("ui.draw_gui", self.asset_resize())` in the uniform try/catch shape. ctx is the display's `asset_resize()` value, the same argument `on_draw_gui` received, so a handler draws in the exact GUI coordinate space the anchor just used, on top of the finished UI.

The emit sits after, and outside, the `DEBUG_TOOLS`/`BUGGER.hide_ui` gate around the anchor call, so it fires on every GUI draw, even while the debug hide-UI flag suppresses the anchor's own draw.

## See Also

- [ui.draw_gui](../hooks/ui.draw_gui.md) - This is the hook this seam dispatches.
- [ui_hud_should_show](ui_hud_should_show.md) - Toggle the engine HUD instead of drawing over it.
- [ui_menu_opened](ui_menu_opened.md) - Track the menus the anchor is drawing.
