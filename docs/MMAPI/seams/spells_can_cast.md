# Seam: spells_can_cast

Puts an override at the head of `can_cast_spell()`.

`spells_can_cast` is a **text seam** (`anchor` + `replace`). It feeds [spells.can_cast](../hooks/spells.can_cast.md). Mod authors never write seams. You register handlers for the hooks they dispatch. See [Seams](../SEAMS.md).

## Placement

| | |
| --- | --- |
| **File** | `gml/scripts/Spells.gml` |
| **Locator** | text anchor: the head of `can_cast_spell(spell)` |
| **Op** | text (`anchor` + `replace`) |
| **Feeds** | [`spells.can_cast`](../hooks/spells.can_cast.md) |
| **ctx built** | `spell` - the spell id |
| **Marker** | `mmapi_spell_can_cast_filter` |

## The Edit

The injected pair of lines runs `mmapi_run_override("spells.can_cast", spell)` at the head of `can_cast_spell(spell)`. If the override chain produces anything other than `undefined`, that value is returned immediately as the entire can-cast answer. None of the engine's checks run, including the mana check. `undefined` falls through to the pristine body, so a deferring handler (or an empty registry) changes nothing.

The engine's own mana check further down the same function reads the spell's cost through the [spells_cost_can_cast](spells_cost_can_cast.md) seam, so a mod that only wants to change the cost should filter [spells.cost](../hooks/spells.cost.md) there rather than override the whole answer here.

## See Also

- [spells.can_cast](../hooks/spells.can_cast.md) - This is the hook this seam dispatches.
- [spells_cost_can_cast](spells_cost_can_cast.md) - This is the cost filter inside the checks this override can bypass.
- [spells_cast_override](spells_cast_override.md) - This seam is the same shape at the head of `cast_spell()`.
