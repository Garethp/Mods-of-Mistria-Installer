# Hook: ui.menu_opened

Know the moment a menu opens.

`ui.menu_opened` is an **event** hook. Register a callback with `mmapi_on`. See [Hooks](../HOOKS.md) for how registration and dispatch work.

## Contract

Fires right after `ANCHOR` spawns a menu and pushes it onto `open_menus`. ctx is `{ menu, kind }`, with `kind` being the menu id passed to `spawn`.

| | |
| --- | --- |
| **Fires** | Right after `ANCHOR` spawns a menu and pushes it onto `open_menus`. |
| **ctx** | `{ menu, kind }` |
| **Kind contract** | The callback observes the moment. Its return value is ignored. |

### The ctx struct

- `menu` - The menu instance `ANCHOR` just spawned, which is already on `open_menus` when the event fires.
- `kind` - the menu id passed to `spawn`. ([ui.menu_closed](ui.menu_closed.md)'s `kind` is read from `menu.type` instead.)

## Usage

```gml
// ui.menu_opened is an EVENT: the return value is ignored.
// You cannot change or stop it here; the return value is ignored.
function menu_greeter_ui_menu_opened(_ctx) {
    // _ctx is { menu, kind }.
    //   .menu - the menu ANCHOR just spawned; already on open_menus.
    //   .kind - the menu id passed to spawn.
    // decorate or track the menu here
}

// inside your latched register function (see Mod Anatomy):
mmapi_on("ui.menu_opened", menu_greeter_ui_menu_opened);
```

## Engine Wiring

- Seam [`ui_menu_opened`](../seams/ui_menu_opened.md) dispatches from `gml/scripts/UI/Anchor/Anchor.gml`, right after `spawn(menu_id, ...)` and the `open_menus.push(menu)`, before the menu is returned.

## See Also

- [ui.menu_closed](ui.menu_closed.md) - Know when a menu closes.
- [ui.menu_refreshed](ui.menu_refreshed.md) - React when a menu rebuilds its content.
