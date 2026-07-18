# Hook: ui.menu_closed

Know when a menu closes.

`ui.menu_closed` is an **event** hook. Register a callback with `mmapi_on`. See [Hooks](../HOOKS.md) for how registration and dispatch work.

## Contract

Fires when a menu leaves `ANCHOR.open_menus`: in the per-frame free-requested drain, and per menu when the anchor shuts down and closes everything. ctx is `{ menu, kind }`, with `kind` read from `menu.type`.

At both sites the menu has already been freed (`free()` in the drain, `on_free()` at shutdown) and removed from `open_menus` when the event fires.

| | |
| --- | --- |
| **Fires** | When a menu leaves `ANCHOR.open_menus`: the per-frame free-requested drain, and per menu at anchor shutdown. |
| **ctx** | `{ menu, kind }` |
| **Kind contract** | The callback observes the moment. Its return value is ignored. |

### The ctx struct

- `menu` - The menu instance that just left `open_menus`. Its free has already run.
- `kind` - the menu's type, read from `menu.type`. ([ui.menu_opened](ui.menu_opened.md)'s `kind` is the menu id passed to `spawn` instead.)

## Usage

```gml
// ui.menu_closed is an EVENT: the return value is ignored.
// You cannot change or stop it here; the return value is ignored.
function menu_janitor_ui_menu_closed(_ctx) {
    // _ctx is { menu, kind }.
    //   .menu - the menu that just left ANCHOR.open_menus; its free has
    //           already run.
    //   .kind - the menu's type, read from menu.type.
    // tear down anything your mod attached to this menu
}

// inside your latched register function (see Mod Anatomy):
mmapi_on("ui.menu_closed", menu_janitor_ui_menu_closed);
```

## Engine Wiring

- Seam [`ui_menu_closed_drain`](../seams/ui_menu_closed_drain.md) dispatches from `gml/scripts/UI/Anchor/Anchor.gml`, in the per-frame free-requested drain, after `menu.free()` and the `open_menus.remove(i)`.
- Seam [`ui_menu_closed_shutdown`](../seams/ui_menu_closed_shutdown.md) dispatches from `gml/scripts/UI/Anchor/Anchor.gml`, in the anchor shutdown loop, once per menu after `menu.on_free()` and the remove.

## See Also

- [ui.menu_opened](ui.menu_opened.md) - Know the moment a menu opens.
- [ui.menu_refreshed](ui.menu_refreshed.md) - React when a menu rebuilds its content.
