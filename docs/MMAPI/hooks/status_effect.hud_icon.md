# Hook: status_effect.hud_icon

Supply the HUD icon for a custom status effect.

`status_effect.hud_icon` is a **filter** hook. Register a callback with `mmapi_filter`. See [Hooks](../HOOKS.md).

## Contract

Fires in `VitalsMenu.refresh_statuses()` for a status effect the base game cannot draw: not a hardcoded vanilla id, not a potion infusion. The filtered value starts `undefined`, and ctx is the `StatusEffectId` ordinal. Return `{ icon_sprite, color }` to draw the icon, where `color` is optional and defaults to the vanilla status orange, or return `undefined` to leave it undrawn. The effect still works either way. Vanilla ids never reach this hook.

```gml
function my_mod_status_icon(_value, _type) {
    if (_type != mmapi_ext_id("status_effect", "my_mod_shroud")) { return undefined; }
    return { icon_sprite: spr_my_mod_shroud_icon };
}

mmapi_filter("status_effect.hud_icon", my_mod_status_icon);
```
