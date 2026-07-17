# Seam: ui_vitals_refreshed

Emits at the tail of `VitalsMenu.refresh_statuses()`, after the status icon strip rebuilds.

`ui_vitals_refreshed` is a **template seam** (`op = "emit"`). It feeds [ui.menu_refreshed](../hooks/ui.menu_refreshed.md). Mod authors never write seams. You register handlers for the hooks they dispatch. See [Seams](../SEAMS.md).

## Placement

| | |
| --- | --- |
| **File** | `gml/scripts/UI/Anchor/Menus/VitalsMenu.gml` |
| **Locator** | pristine context at the tail of `refresh_statuses()`, after the loop that collects active effect types (`array_push(effects, state.status.type)`) |
| **Op** | `emit` |
| **Feeds** | [`ui.menu_refreshed`](../hooks/ui.menu_refreshed.md) |
| **ctx built** | `{ menu: self, kind: self.type }` |
| **Marker** | `mmapi_ui_run_vitals_refreshed` |

## The Edit

`VitalsMenu.refresh_statuses()` rebuilds the vitals HUD's status icon strip. The generated emit lands at its tail, after the loop that collects the active effect types, and runs `mmapi_emit("ui.menu_refreshed", { menu: self, kind: self.type })` in the uniform try/catch shape. It fires on the rebuild edges (a status effect registering, a cancel, and the expiry poll), never per idle frame. `kind` reads from `menu.type`, so one `ui.menu_refreshed` handler can tell vitals rebuilds from toolbar rebuilds.

As with every `ui.menu_refreshed` emit, handlers must be idempotent and must not unconditionally force another rebuild from inside the handler.

## See Also

- [ui.menu_refreshed](../hooks/ui.menu_refreshed.md) - This is the hook this seam dispatches.
- [ui_toolbar_refreshed](ui_toolbar_refreshed.md) - This seam is the twin emit at the tail of `ToolbarMenu.update()`.
