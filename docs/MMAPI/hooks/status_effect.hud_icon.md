# Hook: status_effect.hud_icon

Choose the HUD icon a custom status effect draws.

`status_effect.hud_icon` is a **filter** hook. Register a callback with `mmapi_filter`. See [Hooks](../HOOKS.md) for how registration and dispatch work.

## Contract

Fires in `VitalsMenu.refresh_statuses()`, in the switch's default arm, for a status effect the base game cannot draw, one that is neither a hardcoded vanilla id nor a potion infusion. The filtered value starts `undefined`. ctx is the `StatusEffectId` ordinal of the effect. Return `{ icon_sprite, color }` to draw the icon, where `color` is optional and defaults to the vanilla status orange, or `undefined` to leave the effect undrawn.

| | |
| --- | --- |
| **Fires** | In `VitalsMenu.refresh_statuses()`, in the switch default arm, once per active status effect the engine cannot draw, each time the vitals HUD rebuilds its status strip. |
| **Value** | `undefined`. The engine has no icon for the effect, so there is nothing to start from. |
| **ctx** | The `StatusEffectId` ordinal of the effect. |
| **Kind contract** | The callback receives the current value and returns a replacement, or `undefined` to keep the current value. |

### The ctx value

ctx is a plain ordinal rather than a struct. Compare it against your own id from the generated registry, `mmapi_ext_id("status_effect", "<symbol>")`, and return `undefined` for every other effect.

## Usage

```gml
// status_effect.hud_icon is a FILTER: you receive (value, ctx) and return a
// replacement, or undefined to keep the game's value.
function my_mod_exhaustion_icon(_value, _type) {
    // _value starts undefined: the engine has no icon for this effect.
    // _type is the StatusEffectId ordinal of the effect being drawn.
    if (_type != mmapi_ext_id("status_effect", "you_my_mod_exhaustion")) { return undefined; } // not ours
    return { icon_sprite: spr_ui_hud_info_essence_icon };   // a vanilla sprite, or one the mod ships; color is optional
}

mmapi_filter("status_effect.hud_icon", my_mod_exhaustion_icon);
```

## Engine Wiring

- Seam [`vitals_status_hud_icon`](../seams/vitals_status_hud_icon.md) dispatches from `gml/scripts/UI/Anchor/Menus/VitalsMenu.gml`, in `refresh_statuses()`'s switch default arm, after the infusion lookup fails: it filters `undefined` with the effect's ordinal, skips the effect when the result is still `undefined`, and otherwise reads `icon_sprite` and the optional `color` from the returned struct.

## See Also

- [player.status_effect_register](player.status_effect_register.md) - Rewrite a status effect as it registers.
- [player.status_effect_expired](player.status_effect_expired.md) - Know the moment a status effect runs out.
- [player.status_effect_cancel](player.status_effect_cancel.md) - Know when the game cancels a status effect.
- [Custom Status Effects](../CUSTOM_STATUS_EFFECTS.md) - The guide this hook belongs to, with the registration and the complete example.
