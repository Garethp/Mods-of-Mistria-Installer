# Seam: ui_crafting_refreshed

Emits at the tail of `CraftingMenu.select_category()`, after the recipe grid and scroller rebuild.

`ui_crafting_refreshed` is a **template seam** (`op = "emit"`). It feeds [ui.menu_refreshed](../hooks/ui.menu_refreshed.md). Mod authors never write seams. You register handlers for the hooks they dispatch. See [Seams](../SEAMS.md).

## Placement

| | |
| --- | --- |
| **File** | `gml/scripts/UI/Anchor/Menus/CraftingMenu.gml` |
| **Locator** | pristine context at the tail of `select_category()`, after the grid loop's craftable check |
| **Op** | `emit` |
| **Feeds** | [`ui.menu_refreshed`](../hooks/ui.menu_refreshed.md) |
| **ctx built** | `{ menu: self, kind: self.type }` |
| **Marker** | `mmapi_ui_run_crafting_refreshed` |

## The Edit

`CraftingMenu.select_category()` is the left page's rebuild. It resets `left_pilot`, frees the old scroller, creates a new one, and refills it with the selected category's sub-categories and recipe grid. The generated emit lands after that grid loop, as the last thing `select_category()` does: `mmapi_emit("ui.menu_refreshed", { menu: self, kind: self.type })` in the uniform try/catch shape. It fires on every rebuild edge (the initialize pass after spawn, category tab taps through `carousel_callback()`, and the `run_post_sequence()` refresh after a craft) and never on idle frames.

Because the rebuild frees and recreates the scroller, any scroll position or `left_pilot` selection from before the call is already gone when this fires. `ctx.menu` carries `scroller`, `left_pilot`, and `category_index`, which is what a handler needs to put the view back after the post-craft refresh. The crafting menu's first rebuild runs from `spawn_crafting_menu` after the menu is spawned, so unlike the toolbar and vitals emits this one never fires before [ui.menu_opened](../hooks/ui.menu_opened.md).

## See Also

- [ui.menu_refreshed](../hooks/ui.menu_refreshed.md) - This is the hook this seam dispatches.
- [ui_toolbar_refreshed](ui_toolbar_refreshed.md) - This seam is the sibling emit at the tail of `ToolbarMenu.update()`.
- [ui_vitals_refreshed](ui_vitals_refreshed.md) - This seam is the sibling emit at the tail of `VitalsMenu.refresh_statuses()`.
