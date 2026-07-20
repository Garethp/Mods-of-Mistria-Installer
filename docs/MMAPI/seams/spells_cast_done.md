# Seam: spells_cast_done

Emits at the end of the engine's `cast_spell()`.

`spells_cast_done` is a **template seam** (`op = "emit"`). It feeds [spells.cast_done](../hooks/spells.cast_done.md). Mod authors never write seams. You register handlers for the hooks they dispatch. See [Seams](../SEAMS.md).

## Placement

| | |
| --- | --- |
| **File** | `gml/scripts/Spells.gml` |
| **Locator** | pristine context: after the cast switch closes (`default: impossible("Unexpected Spell: {Spell}", spell);`), at the very end of `cast_spell()` |
| **Op** | `emit` |
| **Feeds** | [`spells.cast_done`](../hooks/spells.cast_done.md) |
| **ctx built** | `spell` - the spell id |
| **Marker** | `mmapi_spell_cast_callback` |

## The Edit

The generated emit lands on the last line of `cast_spell()`, after the engine's cast switch, the one that dispatches each spell id and ends in `default: impossible(...)`, has completed. It calls `mmapi_emit("spells.cast_done", spell)` in the uniform try/catch shape, so handlers see every engine-performed cast the moment its effects have run.

This seam covers only the engine-cast path. When a [spells.cast](../hooks/spells.cast.md) override consumes the cast, `cast_spell()` returns from its head and never reaches this line. That path's `spells.cast_done` emit lives in the [spells_cast_override](spells_cast_override.md) seam instead. Between the two, the event fires exactly once per completed cast, whoever performed it.

## See Also

- [spells.cast_done](../hooks/spells.cast_done.md) - This is the hook this seam dispatches.
- [spells_cast_override](spells_cast_override.md) - This seam is the other `spells.cast_done` emitter, for override-consumed casts.
