# Seam: spells_cost_fsm_default

Filters the mana deduction in the player's default cast state.

`spells_cost_fsm_default` is a **text seam** (`anchor` + `replace`). It feeds [spells.cost](../hooks/spells.cost.md). Mod authors never write seams. You register handlers for the hooks they dispatch. See [Seams](../SEAMS.md).

## Placement

| | |
| --- | --- |
| **File** | `gml/scripts/Player/AriFsm.gml` |
| **Locator** | text anchor: `ARI.modify_mana(-SPELLS[self.spell].cost);` following `cast_spell(self.spell);`, inside the `did_cast_spell == false && self.spell != CUTSCENE_SPELL_SIGNUM` block |
| **Op** | text (`anchor` + `replace`) |
| **Feeds** | [`spells.cost`](../hooks/spells.cost.md) |
| **Value filtered** | `SPELLS[self.spell].cost` - the spell's mana cost |
| **ctx built** | `self.spell` - the spell id |
| **Marker** | `mmapi_spell_default_cost` |

## The Edit

The engine reads a spell's mana cost in four places. This seam wraps the deduction in the player FSM's **default** cast state, the one-shot path that runs `cast_spell(self.spell)` and then charges for it, gated on `did_cast_spell == false` and the spell not being `CUTSCENE_SPELL_SIGNUM`. `ARI.modify_mana(-SPELLS[self.spell].cost)` becomes `ARI.modify_mana(-mmapi_apply_filters("spells.cost", SPELLS[self.spell].cost, self.spell))`: the filter runs on the raw cost before negation, immediately after the `cast_spell` call it pays for.

Because the deduction sits outside `cast_spell()`, it charges the post-filter cost whether the cast was performed by the engine or consumed by a [spells.cast](../hooks/spells.cast.md) override. All four cost reads dispatch the same [spells.cost](../hooks/spells.cost.md) hook with the spell id as ctx. The repeating-cast counterpart of this deduction is [spells_cost_fsm_loop](spells_cost_fsm_loop.md), in the same file.

## See Also

- [spells.cost](../hooks/spells.cost.md) - This is the hook this seam dispatches.
- [spells_cost_fsm_loop](spells_cost_fsm_loop.md) - This seam is the looping cast state's deduction in the same file.
- [spells_cost_can_cast](spells_cost_can_cast.md) - This seam is the cost read in the can-cast mana check.
- [spells_cost_menu](spells_cost_menu.md) - This seam is the cost read behind the spellcasting menu display.
