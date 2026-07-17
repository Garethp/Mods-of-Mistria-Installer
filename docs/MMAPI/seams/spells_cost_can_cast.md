# Seam: spells_cost_can_cast

Filters the mana-cost read inside `can_cast_spell()`'s mana check.

`spells_cost_can_cast` is a **text seam** (`anchor` + `replace`). It feeds [spells.cost](../hooks/spells.cost.md). Mod authors never write seams. You register handlers for the hooks they dispatch. See [Seams](../SEAMS.md).

## Placement

| | |
| --- | --- |
| **File** | `gml/scripts/Spells.gml` |
| **Locator** | text anchor: the `\|\| ARI.get_mana() < SPELLS[spell].cost` clause inside `can_cast_spell()` |
| **Op** | text (`anchor` + `replace`) |
| **Feeds** | [`spells.cost`](../hooks/spells.cost.md) |
| **Value filtered** | `SPELLS[spell].cost` - the spell's mana cost |
| **ctx built** | `spell` - the spell id |
| **Marker** | `mmapi_spell_can_cast_cost` |

## The Edit

The engine reads a spell's mana cost in four places. This seam wraps the first: the mana check in `can_cast_spell()`. The clause `ARI.get_mana() < SPELLS[spell].cost` becomes `ARI.get_mana() < mmapi_apply_filters("spells.cost", SPELLS[spell].cost, spell)`, so whether the player *can* cast is judged against the post-filter cost. The catalog entry (`SPELLS[spell].cost`) itself is never written. The filter runs on each read.

A [spells.cost](../hooks/spells.cost.md) handler is dispatched identically at all four read sites with the spell id as ctx, so one handler keeps the can-cast check, the menu display, and both mana deductions consistent. Note that a [spells.can_cast](../hooks/spells.can_cast.md) override returning early from the head of the same function skips this check, and this filter, entirely.

## See Also

- [spells.cost](../hooks/spells.cost.md) - This is the hook this seam dispatches.
- [spells_cost_menu](spells_cost_menu.md) - This seam is the cost read behind the spellcasting menu display.
- [spells_cost_fsm_loop](spells_cost_fsm_loop.md) - This seam is the mana deduction in the looping cast state.
- [spells_cost_fsm_default](spells_cost_fsm_default.md) - This seam is the mana deduction in the default cast state.
