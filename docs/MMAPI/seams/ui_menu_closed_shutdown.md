# Seam: ui_menu_closed_shutdown

Emits per menu as the anchor shuts down and closes everything.

`ui_menu_closed_shutdown` is a **text seam**, a verbatim `anchor`/`replace` edit. It feeds [ui.menu_closed](../hooks/ui.menu_closed.md). Mod authors never write seams. You register handlers for the hooks they dispatch. See [Seams](../SEAMS.md).

## Placement

| | |
| --- | --- |
| **File** | `gml/scripts/UI/Anchor/Anchor.gml` |
| **Locator** | text anchor on the anchor shutdown loop (`while self.open_menus.count() > 0`) |
| **Feeds** | [`ui.menu_closed`](../hooks/ui.menu_closed.md) |
| **ctx built** | `{ menu: menu, kind: menu.type }` |
| **Marker** | `mmapi_ui_run_menu_closed_shutdown` |

## The Edit

When the anchor shuts down it drains `open_menus` in a `while` loop: take the first menu, call its `on_free()`, remove it from the list. The replace appends one line inside the loop, after the removal: `try { mmapi_emit("ui.menu_closed", { menu: menu, kind: menu.type }); } catch (...)`. One emit fires per closed menu (a shutdown with five menus open fires the hook five times) with the same `{ menu, kind }` ctx shape as the drain path, `kind` read from `menu.type`.

This is the bulk-close half of `ui.menu_closed`'s two dispatch sites. [ui_menu_closed_drain](ui_menu_closed_drain.md) covers the ordinary per-frame free-requested close. Together they fire for every way a menu leaves `open_menus`.

## See Also

- [ui.menu_closed](../hooks/ui.menu_closed.md) - This is the hook this seam dispatches.
- [ui_menu_closed_drain](ui_menu_closed_drain.md) - This seam is the twin emit in the per-frame free-requested drain.
- [ui_menu_opened](ui_menu_opened.md) - This seam is the opening edge.
