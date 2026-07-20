# Seam: ui_toolbar_refreshed

Emits at the tail of `ToolbarMenu.update()`, after the slots re-resolve from the inventory.

`ui_toolbar_refreshed` is a **template seam** (`op = "emit"`). It feeds [ui.menu_refreshed](../hooks/ui.menu_refreshed.md). Mod authors never write seams. You register handlers for the hooks they dispatch. See [Seams](../SEAMS.md).

## Placement

| | |
| --- | --- |
| **File** | `gml/scripts/UI/Anchor/Menus/ToolbarMenu.gml` |
| **Locator** | pristine context at the tail of `update()`, after the slot loop's last `slot.item.set_to_item(inv_slot.item, inv_slot.count)` |
| **Op** | `emit` |
| **Feeds** | [`ui.menu_refreshed`](../hooks/ui.menu_refreshed.md) |
| **ctx built** | `{ menu: self, kind: self.type }` |
| **Marker** | `mmapi_ui_run_toolbar_refreshed` |

## The Edit

`ToolbarMenu.update()` is the toolbar's rebuild: it re-resolves every slot from the inventory (`slot.item.set_to_item(inv_slot.item, inv_slot.count)` per slot). The generated emit lands after that loop, as the last thing `update()` does: `mmapi_emit("ui.menu_refreshed", { menu: self, kind: self.type })` in the uniform try/catch shape. It fires on every rebuild edge (the subscriber pull, page flips, tab taps, forced rebuilds, and the constructor) and never on idle frames.

The constructor's rebuild emits before `ui.menu_opened` fires for the same menu (`ctx.menu` is not yet in `ANCHOR.open_menus` then), and because a rebuild can be forced, handlers must be idempotent and must not unconditionally force another rebuild from inside the handler.

## See Also

- [ui.menu_refreshed](../hooks/ui.menu_refreshed.md) - This is the hook this seam dispatches.
- [ui_vitals_refreshed](ui_vitals_refreshed.md) - This seam is the twin emit at the tail of `VitalsMenu.refresh_statuses()`.
- [ui_toolbar_tick](ui_toolbar_tick.md) - This seam is the same file's per-tick emit, and it fires every tick, rebuild or not.
