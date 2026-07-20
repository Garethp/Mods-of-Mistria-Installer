# Seam: ui_toolbar_tick

Emits every toolbar tick, between the subscriber pull and press-and-hold processing.

`ui_toolbar_tick` is a **template seam** (`op = "emit"`). It feeds [ui.toolbar_tick](../hooks/ui.toolbar_tick.md). Mod authors never write seams. You register handlers for the hooks they dispatch. See [Seams](../SEAMS.md).

## Placement

| | |
| --- | --- |
| **File** | `gml/scripts/UI/Anchor/Menus/ToolbarMenu.gml` |
| **Locator** | pristine context in the toolbar's per-tick body, after the subscriber-pull/update block and before `self.press_and_hold_reader.process();` |
| **Op** | `emit` |
| **Feeds** | [`ui.toolbar_tick`](../hooks/ui.toolbar_tick.md) |
| **ctx built** | `self` - the `ToolbarMenu` |
| **Marker** | `mmapi_ui_run_toolbar_tick` |

## The Edit

The generated emit lands in the toolbar's per-tick body: after `if !self.subscriber.pull().is_empty() { self.update(); }` (the inventory-change pull that triggers a slot rebuild) and before press-and-hold input processing. It calls `mmapi_emit("ui.toolbar_tick", self)` in the uniform try/catch shape. ctx is the live `ToolbarMenu`, so a handler reads its state every tick, after any rebuild for the frame has already happened and before held-press input runs.

## See Also

- [ui.toolbar_tick](../hooks/ui.toolbar_tick.md) - This is the hook this seam dispatches.
- [ui_toolbar_refreshed](ui_toolbar_refreshed.md) - This is the same file's rebuild-edge emit, which fires only when the slots actually re-resolve.
