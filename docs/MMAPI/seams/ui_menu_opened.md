# Seam: ui_menu_opened

Emits the moment the anchor spawns a menu onto `open_menus`.

`ui_menu_opened` is a **template seam** (`op = "emit"`). It feeds [ui.menu_opened](../hooks/ui.menu_opened.md). Mod authors never write seams. You register handlers for the hooks they dispatch. See [Seams](../SEAMS.md).

## Placement

| | |
| --- | --- |
| **File** | `gml/scripts/UI/Anchor/Anchor.gml` |
| **Locator** | pristine context after `var menu = spawn(menu_id, ...)` and `self.open_menus.push(menu)`, before `return menu;` |
| **Op** | `emit` |
| **Feeds** | [`ui.menu_opened`](../hooks/ui.menu_opened.md) |
| **ctx built** | `{ menu: menu, kind: menu_id }` |
| **Marker** | `mmapi_ui_run_menu_opened` |

## The Edit

ANCHOR's open path spawns the menu (`spawn(menu_id, arg1, ..., arg6)`), pushes it onto `open_menus`, and returns it to the caller. The generated emit lands between the push and the return: `mmapi_emit("ui.menu_opened", { menu: menu, kind: menu_id })` in the uniform try/catch shape. `menu` is the just-spawned live menu (fully constructed and already on the open list) and `kind` is the menu id the caller passed to `spawn`, so a handler matches menu kinds without poking at the instance.

## See Also

- [ui.menu_opened](../hooks/ui.menu_opened.md) - This is the hook this seam dispatches.
- [ui_menu_closed_drain](ui_menu_closed_drain.md) - This is the closing edge on the per-frame drain.
- [ui_menu_closed_shutdown](ui_menu_closed_shutdown.md) - This is the closing edge on anchor shutdown.
