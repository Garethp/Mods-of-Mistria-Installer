# Hook: spells.cast

Replace a spell's cast with your own behavior.

`spells.cast` is an **override** hook. Register a callback with `mmapi_override`. See [Hooks](../HOOKS.md) for how registration and dispatch work. This override is **claim-scoped**: many mods may register, but return `undefined` for targets you do not own. Any non-`undefined` return claims the whole interaction.

## Contract

Fires at the top of `cast_spell()`. ctx is the spell id. Return any value other than `undefined` or `false` to consume the cast: the engine cast is skipped and [spells.cast_done](spells.cast_done.md) is emitted. `undefined` defers to the engine cast.

`false` is the quirk case: the override chain stops at the first non-`undefined` return, so `false` ends the chain (no later mod's override runs), but the seam's consume test excludes it, so the engine cast still runs. In effect `false` means "no mod handles this spell, run the engine cast". When you merely do not own the spell, return `undefined` instead so other mods keep their shot.

| | |
| --- | --- |
| **Fires** | At the top of `cast_spell(spell)`, before the engine's spell switch. |
| **ctx** | The spell id. |
| **Kind contract** | The first callback to return a non-`undefined` value replaces the engine's behavior. Return `undefined` to defer. |

### The ctx parameter

- The spell id being cast, the `spell` argument `cast_spell()` received.

> [!NOTE]
> Consuming the cast replaces `cast_spell`'s body only. The mana deduction lives at the player's cast-state call sites and runs after `cast_spell` returns either way. Filter [spells.cost](spells.cost.md) to change what a consumed cast pays.

## Usage

```gml
// spells.cast is an OVERRIDE: return a value to replace the game's whole
// answer; return undefined to let the game (or another mod) decide.
function spell_smith_spells_cast(_ctx) {
    // _ctx is the spell id being cast.
    // if (!spell_smith_owns(_ctx)) return undefined; // defer: not ours   [claim-scoped]
    // <your cast here>
    // return true;   // consume: engine cast skipped, spells.cast_done fires
    // return false;  // quirk: ends the override chain, engine cast still runs
    return undefined; // defer to the engine (or another mod)
}

mmapi_override("spells.cast", spell_smith_spells_cast);
```

## Engine Wiring

- Seam [`spells_cast_override`](../seams/spells_cast_override.md) dispatches from `gml/scripts/Spells.gml`, at the head of `cast_spell(spell)`: on a return that is neither `undefined` nor `false` it emits `spells.cast_done` (in its own try/catch) and returns before the engine's spell switch.

## See Also

- [spells.cast_done](spells.cast_done.md) - This hook fires for engine casts and consumed casts alike.
- [spells.can_cast](spells.can_cast.md) - Take over whether the spell can be cast at all.
- [spells.cost](spells.cost.md) - Change the mana the cast states deduct.
