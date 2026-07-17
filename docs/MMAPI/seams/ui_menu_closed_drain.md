# Seam: ui_menu_closed_drain

Emits when the per-frame drain removes a free-requested menu.

`ui_menu_closed_drain` is a **template seam** (`op = "emit"`). It feeds [ui.menu_closed](../hooks/ui.menu_closed.md). Mod authors never write seams. You register handlers for the hooks they dispatch. See [Seams](../SEAMS.md).

## Placement

| | |
| --- | --- |
| **File** | `gml/scripts/UI/Anchor/Anchor.gml` |
| **Locator** | pristine context inside the free-requested drain loop, after `menu.free()` and `self.open_menus.remove(i)`, before `i -= 1` |
| **Op** | `emit` |
| **Feeds** | [`ui.menu_closed`](../hooks/ui.menu_closed.md) |
| **ctx built** | `{ menu: menu, kind: menu.type }` |
| **Marker** | `mmapi_ui_run_menu_closed_drain` |

## The Edit

Menus close by requesting a free, and each frame ANCHOR drains them: for every menu with `free_requested` it calls `menu.free()`, removes it from `open_menus`, and steps the loop index back. The generated emit lands right after the removal (`mmapi_emit("ui.menu_closed", { menu: menu, kind: menu.type })` in the uniform try/catch shape), so by the time a handler sees the menu it has been freed and is already off the open list. `kind` reads from `menu.type`.

This is the normal-close half of `ui.menu_closed`'s two dispatch sites. [ui_menu_closed_shutdown](ui_menu_closed_shutdown.md) covers menus closed in bulk when the anchor shuts down. Together they fire for every way a menu leaves `open_menus`.

## See Also

- [ui.menu_closed](../hooks/ui.menu_closed.md) - This is the hook this seam dispatches.
- [ui_menu_closed_shutdown](ui_menu_closed_shutdown.md) - This is the twin emit in the anchor shutdown loop.
- [ui_menu_opened](ui_menu_opened.md) - This seam is the opening edge.
