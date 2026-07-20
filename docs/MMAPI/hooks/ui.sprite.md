# Hook: ui.sprite

Swap the backplate sprites behind the mines menu and spell cards.

`ui.sprite` is a **filter** hook. Register a callback with `mmapi_filter`. See [Hooks](../HOOKS.md) for how registration and dispatch work.

## Contract

Fires at backplate sprite assignments. `ctx.source` names the site: `mines_menu_backplate` (the mines menu backplate on dungeon room start, ctx `{ source }`) and `spellcasting_card_backplate` (the spell card backplate, ctx `{ source, spell }`). The filtered value is the default backplate sprite. Return the replacement sprite, or `undefined` to keep the current value.

| | |
| --- | --- |
| **Fires** | At backplate sprite assignments: the mines menu backplate on dungeon room start, and the spell card backplate. |
| **Value** | The default backplate sprite: `spr_ui_dungeon_backplate` at the mines site, `spr_ui_journal_magic_card_backplate` at the spell card site. |
| **ctx** | `{ source }` (mines) / `{ source, spell }` (spell card) |
| **Kind contract** | The callback receives the current value and returns a replacement, or `undefined` to keep the current value. |

### The ctx struct

- `source` - names the dispatch site: `"mines_menu_backplate"` or `"spellcasting_card_backplate"`.
- `spell` - the spell whose card is being built (spell card site only).

## Usage

```gml
// ui.sprite is a FILTER: you receive (value, ctx) and return a
// replacement, or undefined to keep the game's value.
function deco_backplates_ui_sprite(_value, _ctx) {
    // _value is the default backplate sprite.
    // _ctx is { source } at the mines site, { source, spell } at the
    // spell card site.
    //   .source - "mines_menu_backplate" or "spellcasting_card_backplate".
    //   .spell  - the spell whose card is being built (spell card site only).
    if (_value == undefined) return undefined; // test undefined BEFORE anything else
    // if (_ctx.source == "mines_menu_backplate") {
    //     return spr_deco_backplates_mines;
    // }
    return undefined; // undefined = keep the game's value
}

mmapi_filter("ui.sprite", deco_backplates_ui_sprite);
```

## Engine Wiring

- Seam [`ui_sprite_mines_backplate`](../seams/ui_sprite_mines_backplate.md) dispatches from `gml/scripts/UI/Anchor/Menus/MinesMenu.gml`, in the room start callback: on dungeon room start it filters `spr_ui_dungeon_backplate` and sets the result before `self.backplate.enable()`.
- Seam [`ui_sprite_spell_card_backplate`](../seams/ui_sprite_spell_card_backplate.md) dispatches from `gml/scripts/UI/Anchor/Menus/SpellcastingMenu.gml`, filtering `spr_ui_journal_magic_card_backplate` as the card's sprite is set.

## See Also

- [ui.button_sprites](ui.button_sprites.md) - Swap the sprite set a UI button is built from.
- [ui.item_icon](ui.item_icon.md) - Swap the sprite an item shows as its icon.
- [spells.cost](spells.cost.md) - Change the mana cost the spell card displays (and pays).
