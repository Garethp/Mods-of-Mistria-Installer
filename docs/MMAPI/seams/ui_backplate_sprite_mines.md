# Seam: ui_backplate_sprite_mines

Routes the mines menu backplate sprite through a filter on dungeon room start.

`ui_backplate_sprite_mines` is a **text seam**, a verbatim `anchor`/`replace` edit. It feeds [ui.backplate_sprite](../hooks/ui.backplate_sprite.md). Mod authors never write seams. You register handlers for the hooks they dispatch. See [Seams](../SEAMS.md).

## Placement

| | |
| --- | --- |
| **File** | `gml/scripts/UI/Anchor/Menus/MinesMenu.gml` |
| **Locator** | text anchor in the menu's room-start callback, in the `is_dungeon_room(room())` branch, before `self.backplate.enable()` |
| **Feeds** | [`ui.backplate_sprite`](../hooks/ui.backplate_sprite.md) |
| **Value filtered** | `spr_ui_dungeon_backplate`, the default backplate sprite |
| **ctx built** | `{ source: "mines_menu_backplate" }` |
| **Marker** | `mmapi_mines_backplate_sprite` |

## The Edit

The pristine room-start callback just enables the mines backplate when a dungeon room starts. The replace inserts, before the enable, a try/catch that assigns the backplate's sprite through the filter: `self.backplate.set_sprite(mmapi_apply_filters("ui.backplate_sprite", spr_ui_dungeon_backplate, { source: "mines_menu_backplate" }))`. The default `spr_ui_dungeon_backplate` rides in the value position. A handler matching `ctx.source == "mines_menu_backplate"` returns a replacement plate, and the assignment re-runs on every dungeon room start.

With zero handlers the injected line re-assigns the default sprite the backplate already carries, then the pristine enable runs as before.

## See Also

- [ui.backplate_sprite](../hooks/ui.backplate_sprite.md) - This is the hook this seam dispatches.
- [ui_backplate_sprite_spell_card](ui_backplate_sprite_spell_card.md) - This seam is the other `ui.backplate_sprite` site, on the spell card backplate.
