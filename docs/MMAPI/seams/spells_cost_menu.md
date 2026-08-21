# Seam: spells_cost_menu

Filters the mana-cost read behind the spellcasting menu's cost display and renders the result at quarter granularity.

`spells_cost_menu` is a **text seam** (`anchor` + `replace`). It feeds [spells.cost](../hooks/spells.cost.md). Mod authors never write seams. You register handlers for the hooks they dispatch. See [Seams](../SEAMS.md).

## Placement

| | |
| --- | --- |
| **File** | `gml/scripts/UI/Anchor/Menus/SpellcastingMenu.gml` |
| **Locator** | text anchor: the card's cost render block, from `var cost = self.spell_data.cost div 4;` through the orb stamp loop |
| **Op** | text (`anchor` + `replace`) |
| **Feeds** | [`spells.cost`](../hooks/spells.cost.md) |
| **Value filtered** | `self.spell_data.cost` - the spell's raw mana cost |
| **ctx built** | `spell` - the spell id |
| **Marker** | `mmapi_spell_menu_cost` |

## The Edit

The engine reads a spell's mana cost in four places. This seam wraps the display read in the spellcasting menu and widens the card's cost rendering to quarter granularity. Pristine computes `var cost = self.spell_data.cost div 4;` and stamps that many full-orb sprites, so any cost that is not a multiple of 4 truncates. A cost of 6 drew one orb against a real drain of one and a half, and a cost below 4 drew nothing at all.

The replacement applies the filter to the **raw** cost. `mmapi_apply_filters("spells.cost", self.spell_data.cost, spell)` runs once, and the result splits into full orbs (`cost div 4`) and a remainder. The card stamps one full orb per 4 mana, then one partial orb for any remainder, chosen by the same ladder the vitals HUD uses in `set_mana`: a remainder at or below 1 draws `spr_ui_hud_health_mana_ball_threethirds`, at or below 2 the `half` sprite, at or below 3 the `onequarter` sprite, and anything above 3 a full orb. The sprite names describe the drained portion of the orb rather than the fill, so the quarter-full orb really is the `threethirds` sprite. Because the ladder reuses the HUD's comparisons, a non-integer filtered cost rounds up to the next quarter exactly as the HUD would draw the same mana value, and the spell card can never disagree with the vitals bar about what a fraction looks like.

Because all four cost reads dispatch the same [spells.cost](../hooks/spells.cost.md) hook with the spell id as ctx, one handler keeps the menu display consistent with the can-cast check and the two mana deductions. Return the same replacement everywhere and the UI never lies about what a cast will drain.

## See Also

- [spells.cost](../hooks/spells.cost.md) - This is the hook this seam dispatches.
- [spells_cost_can_cast](spells_cost_can_cast.md) - This is the cost read in the can-cast mana check.
- [spells_cost_fsm_loop](spells_cost_fsm_loop.md) - This is the mana deduction in the looping cast state.
- [spells_cost_fsm_default](spells_cost_fsm_default.md) - This is the mana deduction in the default cast state.
- [ui_backplate_sprite_spell_card](ui_backplate_sprite_spell_card.md) - This is the same menu's card backplate sprite filter.
