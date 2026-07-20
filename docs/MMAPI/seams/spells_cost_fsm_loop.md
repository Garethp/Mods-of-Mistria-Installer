# Seam: spells_cost_fsm_loop

Filters the mana deduction in the player's looping cast state.

`spells_cost_fsm_loop` is a **text seam** (`anchor` + `replace`). It feeds [spells.cost](../hooks/spells.cost.md). Mod authors never write seams. You register handlers for the hooks they dispatch. See [Seams](../SEAMS.md).

## Placement

| | |
| --- | --- |
| **File** | `gml/scripts/Player/AriFsm.gml` |
| **Locator** | text anchor: `ARI.modify_mana(-SPELLS[self.spell].cost);` inside the `if !MIST.is_running()` block of the looping cast state |
| **Op** | text (`anchor` + `replace`) |
| **Feeds** | [`spells.cost`](../hooks/spells.cost.md) |
| **Value filtered** | `SPELLS[self.spell].cost` - the spell's mana cost |
| **ctx built** | `self.spell` - the spell id |
| **Marker** | `mmapi_spell_loop_cost` |

## The Edit

The engine reads a spell's mana cost in four places. This seam wraps the deduction in the player FSM's **looping** cast state, the one gated on `!MIST.is_running()`. `ARI.modify_mana(-SPELLS[self.spell].cost)` becomes `ARI.modify_mana(-mmapi_apply_filters("spells.cost", SPELLS[self.spell].cost, self.spell))`: the filter runs on the raw cost before negation, so the value a handler returns is exactly how much mana the loop tick drains.

All four cost reads dispatch the same [spells.cost](../hooks/spells.cost.md) hook with the spell id as ctx. A handler that returns the same replacement everywhere keeps this deduction in step with the can-cast check and the menu display. The sibling deduction for one-shot casts lives in [spells_cost_fsm_default](spells_cost_fsm_default.md), a few lines away in the same file.

## See Also

- [spells.cost](../hooks/spells.cost.md) - This is the hook this seam dispatches.
- [spells_cost_fsm_default](spells_cost_fsm_default.md) - This seam is the default cast state's deduction in the same file.
- [spells_cost_can_cast](spells_cost_can_cast.md) - This seam is the cost read in the can-cast mana check.
- [spells_cost_menu](spells_cost_menu.md) - This seam is the cost read behind the spellcasting menu display.
