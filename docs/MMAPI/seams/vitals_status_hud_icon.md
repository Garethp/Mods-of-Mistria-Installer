# Seam: vitals_status_hud_icon

Filters the vitals HUD's icon for a status effect the base game cannot draw.

`vitals_status_hud_icon` is a **text seam** (`anchor` + `replace`). It feeds [status_effect.hud_icon](../hooks/status_effect.hud_icon.md). Mod authors never write seams. You register handlers for the hooks they dispatch. See [Seams](../SEAMS.md).

## Placement

| | |
| --- | --- |
| **File** | `gml/scripts/UI/Anchor/Menus/VitalsMenu.gml` |
| **Locator** | text anchor on the `default` arm of the type switch in `refresh_statuses()` |
| **Op** | text (`anchor` + `replace`) |
| **Feeds** | [`status_effect.hud_icon`](../hooks/status_effect.hud_icon.md) |
| **Value filtered** | `undefined`, since the engine has no icon for the effect |
| **ctx built** | `state.status.type`, the `StatusEffectId` ordinal of the effect |
| **Marker** | `mmapi_status_hud_icon` |

## The Edit

Pristine `refresh_statuses()` walks the player's active status effects and picks an icon and a color for each in a switch over the effect's type. The hardcoded vanilla effects have arms of their own, and the `default` arm treats every other type as a potion infusion. It converts the type's name with the fatal `string_to_infusion` and reads the infusion's tooltip icon and color. A custom status effect reaching that arm would crash the menu, because its name is not an infusion.

The replacement converts with `try_string_to_infusion` first. A hit takes the pristine path unchanged, and every type the base game can produce is a hit. A miss dispatches the filter with `undefined` and the effect's ordinal. A handler that returns `{ icon_sprite, color }` supplies the icon, and the color too when the struct carries one. A result that is still `undefined` skips the effect, so an effect with no handler draws nothing instead of crashing.

With zero handlers on an intact install the seam is behaviorally identical to pristine, since only infusions reach the `default` arm and each of them is a hit. The dispatch runs once per active effect the base game cannot draw, each time the vitals HUD rebuilds its status strip.

## See Also

- [status_effect.hud_icon](../hooks/status_effect.hud_icon.md) - This is the hook this seam dispatches.
- [status_effect](../extensions/status_effect.md) - The extension point whose effects reach this arm.
- [save_load_status_effect_tolerance](save_load_status_effect_tolerance.md) - The load-side companion, which drops an effect whose type no longer resolves.
