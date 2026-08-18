# Seam: ui_backplate_sprite_spell_card

Routes each spell card's backplate sprite through a filter.

`ui_backplate_sprite_spell_card` is a **text seam**, a verbatim `anchor`/`replace` edit. It feeds [ui.backplate_sprite](../hooks/ui.backplate_sprite.md). Mod authors never write seams. You register handlers for the hooks they dispatch. See [Seams](../SEAMS.md).

## Placement

| | |
| --- | --- |
| **File** | `gml/scripts/UI/Anchor/Menus/SpellcastingMenu.gml` |
| **Locator** | text anchor on the card's `set_sprite(spr_ui_journal_magic_card_backplate)` call |
| **Feeds** | [`ui.backplate_sprite`](../hooks/ui.backplate_sprite.md) |
| **Value filtered** | `spr_ui_journal_magic_card_backplate`, the default spell card backplate |
| **ctx built** | `{ source: "spellcasting_card_backplate", spell: spell }` |
| **Marker** | `mmapi_spell_card_backplate` |

## The Edit

A one-line rewrite in the spellcasting menu's card build: `self.card.set_sprite(spr_ui_journal_magic_card_backplate)` becomes `self.card.set_sprite(mmapi_apply_filters("ui.backplate_sprite", spr_ui_journal_magic_card_backplate, { source: "spellcasting_card_backplate", spell: spell }))`. The default backplate rides in the value position. Unlike the mines site, the ctx carries the `spell` id alongside `source`, so a handler can re-skin the card per spell (a custom spell gets a custom plate) and return `undefined` for spells it does not own.

## See Also

- [ui.backplate_sprite](../hooks/ui.backplate_sprite.md) - This is the hook this seam dispatches.
- [ui_backplate_sprite_mines](ui_backplate_sprite_mines.md) - This is the other `ui.backplate_sprite` site, on the mines menu backplate.
