# Hook: ui.toolbar_tick

React on every toolbar tick.

`ui.toolbar_tick` is an **event** hook. Register a callback with `mmapi_on`. See [Hooks](../HOOKS.md) for how registration and dispatch work.

## Contract

Fires every toolbar tick, after the subscriber pull and update and before press-and-hold processing. ctx is the `ToolbarMenu`.

| | |
| --- | --- |
| **Fires** | Every toolbar tick, after the subscriber pull and update and before press-and-hold processing. |
| **ctx** | The `ToolbarMenu`. |
| **Kind contract** | The callback observes the moment. Its return value is ignored. |

### The ctx parameter

- The `ToolbarMenu` itself, mid-tick: the subscriber pull (and any `update()` it triggered) has run, press-and-hold processing has not.

## Usage

```gml
// ui.toolbar_tick is an EVENT: mmapi calls you after it happens.
// You cannot change or stop it here; the return value is ignored.
function quick_swap_ui_toolbar_tick(_ctx) {
    // _ctx is the ToolbarMenu.
    // Runs every toolbar tick, after the subscriber pull (and the update()
    // it may trigger) and before press-and-hold processing.
}

// inside your latched register function (see Mod Anatomy):
mmapi_on("ui.toolbar_tick", quick_swap_ui_toolbar_tick);
```

## Engine Wiring

- Seam [`ui_toolbar_tick`](../seams/ui_toolbar_tick.md) dispatches from `gml/scripts/UI/Anchor/Menus/ToolbarMenu.gml`, between the subscriber pull (and the `update()` it may trigger) and `press_and_hold_reader.process()`.

## See Also

- [ui.menu_refreshed](ui.menu_refreshed.md) - React on the toolbar's rebuild edges instead of every tick.
- [ui.draw_gui](ui.draw_gui.md) - React to every GUI draw with your own overlay.
