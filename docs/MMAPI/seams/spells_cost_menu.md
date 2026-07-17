# Seam: spells_cost_menu

Filters the mana-cost read behind the spellcasting menu's cost display.

`spells_cost_menu` is a **text seam** (`anchor` + `replace`). It feeds [spells.cost](../hooks/spells.cost.md). Mod authors never write seams. You register handlers for the hooks they dispatch. See [Seams](../SEAMS.md).

## Placement

| | |
| --- | --- |
| **File** | `gml/scripts/UI/Anchor/Menus/SpellcastingMenu.gml` |
| **Locator** | text anchor: `var cost = self.spell_data.cost div 4;` in the menu's card rendering |
| **Op** | text (`anchor` + `replace`) |
| **Feeds** | [`spells.cost`](../hooks/spells.cost.md) |
| **Value filtered** | `self.spell_data.cost` - the spell's raw mana cost |
| **ctx built** | `spell` - the spell id |
| **Marker** | `mmapi_spell_menu_cost` |

## The Edit

The engine reads a spell's mana cost in four places. This seam wraps the display read in the spellcasting menu. `var cost = self.spell_data.cost div 4;` becomes `var cost = mmapi_apply_filters("spells.cost", self.spell_data.cost, spell) div 4;`. The filter sees the **raw** cost, and the menu's `div 4` scaling is applied to whatever the filter returns. The number a player reads on the spell card is therefore a quarter of the post-filter cost, matching how the raw cost relates to the displayed one in pristine.

Because all four cost reads dispatch the same [spells.cost](../hooks/spells.cost.md) hook with the spell id as ctx, one handler keeps the menu display consistent with the can-cast check and the two mana deductions. Return the same replacement everywhere and the UI never lies about what a cast will drain.

## See Also

- [spells.cost](../hooks/spells.cost.md) - This is the hook this seam dispatches.
- [spells_cost_can_cast](spells_cost_can_cast.md) - This is the cost read in the can-cast mana check.
- [spells_cost_fsm_loop](spells_cost_fsm_loop.md) - This is the mana deduction in the looping cast state.
- [spells_cost_fsm_default](spells_cost_fsm_default.md) - This is the mana deduction in the default cast state.
- [ui_sprite_spell_card_backplate](ui_sprite_spell_card_backplate.md) - This is the same menu's card backplate sprite filter.
