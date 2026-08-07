# Seam: spells_cast_override

Puts an override at the head of `cast_spell()` that can consume the whole cast.

`spells_cast_override` is a **text seam** (`anchor` + `replace`). It feeds [spells.cast](../hooks/spells.cast.md) and [spells.cast_done](../hooks/spells.cast_done.md). Mod authors never write seams. You register handlers for the hooks they dispatch. See [Seams](../SEAMS.md).

## Placement

| | |
| --- | --- |
| **File** | `gml/scripts/Spells.gml` |
| **Locator** | text anchor: the head of `cast_spell(spell)` |
| **Op** | text (`anchor` + `replace`) |
| **Feeds** | [`spells.cast`](../hooks/spells.cast.md), [`spells.cast_done`](../hooks/spells.cast_done.md) |
| **ctx built** | `spell` - the spell id |
| **Marker** | `mmapi_spell_cast_override` |

## The Edit

The injected block runs `mmapi_run_override("spells.cast", spell)` at the head of `cast_spell(spell)` and inspects the result. Any result other than `undefined` consumes the cast: the block emits [spells.cast_done](../hooks/spells.cast_done.md) with the spell id (in its own try/catch, so a throwing event handler cannot break the return) and then returns. The engine's cast switch never runs. That emit is the override-consumed path of `spells.cast_done`. The engine-cast path is the separate [spells_cast_done](spells_cast_done.md) seam at the end of the same function, so the event fires exactly once per completed cast either way.

`undefined` is the only deferral: it lets later overrides run, and the engine cast proceeds when every handler declines. The consume test is type-exact (`typeof`), matching the dispatcher's own decline rule, so Boolean `false` and numeric zero consume like any other defined result. Note that the caller in the player FSM deducts mana around `cast_spell` through the [spells.cost](../hooks/spells.cost.md) filter regardless of who handled the cast.

## See Also

- [spells.cast](../hooks/spells.cast.md) - This is the override hook this seam dispatches.
- [spells.cast_done](../hooks/spells.cast_done.md) - This is the event this seam emits when an override consumes the cast.
- [spells_cast_done](spells_cast_done.md) - This is the engine-cast completion emit at the end of the same function.
- [spells_can_cast](spells_can_cast.md) - This is the same shape at the head of `can_cast_spell()`.
