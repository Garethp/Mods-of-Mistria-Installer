# Hook: ui.menu_refreshed

React when a menu rebuilds its content.

`ui.menu_refreshed` is an **event** hook. Register a callback with `mmapi_on`. See [Hooks](../HOOKS.md) for how registration and dispatch work.

## Contract

Fires after a menu rebuilds its content from game state, at the tail of the rebuild function. Three menus emit it today: `ToolbarMenu.update()` (slot items re-resolved from the inventory: subscriber pull, page flips, tab taps, forced rebuilds, constructor), `VitalsMenu.refresh_statuses()` (status icon strip rebuilt: register, cancel, and the expiry poll), and `CraftingMenu.select_category()` (recipe grid rebuilt onto a fresh scroller: the initialize pass after spawn, category tab taps, and the post-craft refresh).

ctx is `{ menu, kind }`, with `kind` read from `menu.type`. Emits only on rebuild edges, never per idle frame. The toolbar and vitals constructors run their rebuild, so those two menus can emit before [ui.menu_opened](ui.menu_opened.md) fires for them (`ctx.menu` is not yet in `ANCHOR.open_menus` then). The crafting menu's first rebuild is the initialize pass `spawn_crafting_menu` runs after spawn, so its emits always come with the menu already open. Handlers must be idempotent and must not unconditionally force another rebuild from inside the handler.

| | |
| --- | --- |
| **Fires** | At the tail of a menu's rebuild function, after its content is rebuilt from game state. |
| **ctx** | `{ menu, kind }` |
| **Kind contract** | The callback observes the moment. Its return value is ignored. |

### The ctx struct

- `menu` - the menu that just rebuilt its content. During a constructor rebuild it is not yet in `ANCHOR.open_menus`.
- `kind` - the menu's type, read from `menu.type`.

> [!IMPORTANT]
> This can fire during a menu's constructor, before `ui.menu_opened` (the toolbar and vitals rebuilds do). `ctx.menu` is not yet in `ANCHOR.open_menus` then. Handlers must be idempotent and must not unconditionally force another rebuild from inside the handler.

## Usage

```gml
// ui.menu_refreshed is an EVENT: the return value is ignored.
// You cannot change or stop it here; the return value is ignored.
function toolbar_badges_ui_menu_refreshed(_ctx) {
    // _ctx is { menu, kind }.
    //   .menu - the menu that just rebuilt its content. During a
    //           constructor rebuild it is not yet in ANCHOR.open_menus.
    //   .kind - the menu's type, read from menu.type.
    // Re-apply your decorations here. Be idempotent: this fires on every
    // rebuild edge, and forcing another rebuild from here loops.
}

// inside your latched register function (see Mod Anatomy):
mmapi_on("ui.menu_refreshed", toolbar_badges_ui_menu_refreshed);
```

## Engine Wiring

- Seam [`ui_toolbar_refreshed`](../seams/ui_toolbar_refreshed.md) dispatches from `gml/scripts/UI/Anchor/Menus/ToolbarMenu.gml`, at the tail of `update()`, after the slot loop re-resolves items from the inventory.
- Seam [`ui_vitals_refreshed`](../seams/ui_vitals_refreshed.md) dispatches from `gml/scripts/UI/Anchor/Menus/VitalsMenu.gml`, at the tail of `refresh_statuses()`, after the status icon strip is rebuilt.
- Seam [`ui_crafting_refreshed`](../seams/ui_crafting_refreshed.md) dispatches from `gml/scripts/UI/Anchor/Menus/CraftingMenu.gml`, at the tail of `select_category()`, after the recipe grid and scroller are rebuilt.

## See Also

- [ui.menu_opened](ui.menu_opened.md) - Know the moment a menu opens.
- [ui.menu_closed](ui.menu_closed.md) - Know when a menu closes.
- [ui.toolbar_tick](ui.toolbar_tick.md) - React on every toolbar tick, rebuild or not.
