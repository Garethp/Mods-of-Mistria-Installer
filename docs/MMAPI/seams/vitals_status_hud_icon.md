# Seam: vitals_status_hud_icon

Makes the vitals HUD's status-icon default arm safe for non-infusion ids and mod-suppliable.

`vitals_status_hud_icon` is a **text seam** (filter-shaped). It feeds [status_effect.hud_icon](../hooks/status_effect.hud_icon.md). See [Seams](../SEAMS.md).

## Placement

| | |
| --- | --- |
| **File** | `gml/scripts/UI/Anchor/Menus/VitalsMenu.gml` |
| **Locator** | text anchor on `refresh_statuses()`'s switch default arm |
| **Feeds** | [`status_effect.hud_icon`](../hooks/status_effect.hud_icon.md) |
| **Marker** | `mmapi_status_hud_icon` |

## Why

Vanilla's default arm assumes every non-hardcoded status id is a potion infusion and resolves it through the fatal `string_to_infusion`, so an [extension status effect](../extensions/status_effect.md) shown on the HUD would crash the menu. The seam swaps in `try_string_to_infusion`: a hit takes the vanilla path pixel-identically (every id vanilla can produce is a hit), a miss asks the hook, and no handler skips the icon rather than crashing.
