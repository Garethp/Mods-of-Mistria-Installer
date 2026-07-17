# Seam: ui_sprite_mines_backplate

Routes the mines menu backplate sprite through a filter on dungeon room start.

`ui_sprite_mines_backplate` is a **text seam**, a verbatim `anchor`/`replace` edit. It feeds [ui.sprite](../hooks/ui.sprite.md). Mod authors never write seams. You register handlers for the hooks they dispatch. See [Seams](../SEAMS.md).

## Placement

| | |
| --- | --- |
| **File** | `gml/scripts/UI/Anchor/Menus/MinesMenu.gml` |
| **Locator** | text anchor in the menu's room-start callback, in the `is_dungeon_room(room())` branch, before `self.backplate.enable()` |
| **Feeds** | [`ui.sprite`](../hooks/ui.sprite.md) |
| **Value filtered** | `spr_ui_dungeon_backplate`, the default backplate sprite |
| **ctx built** | `{ source: "mines_menu_backplate" }` |
| **Marker** | `mmapi_mines_backplate_sprite` |

## The Edit

The pristine room-start callback just enables the mines backplate when a dungeon room starts. The replace inserts, before the enable, a try/catch that assigns the backplate's sprite through the filter: `self.backplate.set_sprite(mmapi_apply_filters("ui.sprite", spr_ui_dungeon_backplate, { source: "mines_menu_backplate" }))`. The default `spr_ui_dungeon_backplate` rides in the value position. A handler matching `ctx.source == "mines_menu_backplate"` returns a replacement plate, and the assignment re-runs on every dungeon room start.

With zero handlers the injected line re-assigns the default sprite the backplate already carries, then the pristine enable runs as before.

## See Also

- [ui.sprite](../hooks/ui.sprite.md) - This is the hook this seam dispatches.
- [ui_sprite_spell_card_backplate](ui_sprite_spell_card_backplate.md) - This seam is the other `ui.sprite` site, on the spell card backplate.
