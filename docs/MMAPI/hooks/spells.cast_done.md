# Hook: spells.cast_done

Know when a spell cast completes.

`spells.cast_done` is an **event** hook. Register a callback with `mmapi_on`. See [Hooks](../HOOKS.md) for how registration and dispatch work.

## Contract

Fires when a spell cast completes: at the end of the engine's `cast_spell()`, and also when a [spells.cast](spells.cast.md) override consumes the cast. ctx is the spell id. One hook, both completion paths, a handler here counts every cast, engine-run or mod-run.

| | |
| --- | --- |
| **Fires** | At the end of the engine's `cast_spell()`, and when a `spells.cast` override consumes the cast. |
| **ctx** | The spell id. |
| **Kind contract** | The callback observes the moment. Its return value is ignored. |

### The ctx parameter

- The spell id that just finished casting.

## Usage

```gml
// spells.cast_done is an EVENT: mmapi calls you after it happens.
// You cannot change or stop it here; the return value is ignored.
function spell_tally_spells_cast_done(_ctx) {
    // _ctx is the spell id that just finished casting. Fires for engine
    // casts and for casts consumed by a spells.cast override alike.
    // your code here, e.g. count casts per spell
}

// inside your latched register function (see Mod Anatomy):
mmapi_on("spells.cast_done", spell_tally_spells_cast_done);
```

## Engine Wiring

- Seam [`spells_cast_done`](../seams/spells_cast_done.md) dispatches from `gml/scripts/Spells.gml`, at the tail of `cast_spell()`, after the engine's spell switch completes.
- Seam [`spells_cast_override`](../seams/spells_cast_override.md) also emits it from the head of `cast_spell()`, when a `spells.cast` override consumes the cast.

## See Also

- [spells.cast](spells.cast.md) - This hook is the override whose consumed casts also land here.
- [spells.can_cast](spells.can_cast.md) - Take over whether a spell can be cast.
- [spells.cost](spells.cost.md) - Change a spell's mana cost everywhere the engine reads it.
