# Hook: spells.can_cast

Take over whether a spell can be cast.

`spells.can_cast` is an **override** hook. Register a callback with `mmapi_override`. See [Hooks](../HOOKS.md) for how registration and dispatch work. This override is **claim-scoped**: many mods may register, but return `undefined` for targets you do not own. Any non-`undefined` return claims the whole interaction.

## Contract

Fires at the top of `can_cast_spell()`. ctx is the spell id. Return a non-`undefined` value to replace the entire can-cast result. `undefined` defers to the normal checks, including the mana test, which [spells.cost](spells.cost.md) filters. A claim skips every engine check, so `true` here makes the spell castable regardless of mana.

| | |
| --- | --- |
| **Fires** | At the top of `can_cast_spell(spell)`, before any engine check runs. |
| **ctx** | The spell id. |
| **Kind contract** | The first callback to return a non-`undefined` value replaces the engine's behavior. Return `undefined` to defer. |

### The ctx parameter

- This is the spell id being tested, the `spell` argument `can_cast_spell()` received.

## Usage

```gml
// spells.can_cast is an OVERRIDE: return a value to replace the game's whole
// answer; return undefined to let the game (or another mod) decide.
function arcane_license_spells_can_cast(_ctx) {
    // _ctx is the spell id being tested.
    // if (!arcane_license_owns(_ctx)) return undefined; // defer: not ours   [claim-scoped]
    // return true; // castable regardless of mana or any engine check
    return undefined;
}

mmapi_override("spells.can_cast", arcane_license_spells_can_cast);
```

## Engine Wiring

- Seam [`spells_can_cast`](../seams/spells_can_cast.md) dispatches from `gml/scripts/Spells.gml`, at the head of `can_cast_spell(spell)`. A non-`undefined` override return becomes `can_cast_spell`'s return value.

## See Also

- [spells.cast](spells.cast.md) - Replace the cast itself.
- [spells.cost](spells.cost.md) - Change the mana cost the deferred-to engine check reads.
- [spells.cast_done](spells.cast_done.md) - Know when a spell cast completes.
