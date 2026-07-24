# Hook: spells.cost

Change a spell's mana cost everywhere the engine reads it.

`spells.cost` is a **filter** hook. Register a callback with `mmapi_filter`. See [Hooks](../HOOKS.md) for how registration and dispatch work.

## Contract

Fires wherever the engine reads a spell's mana cost: the `can_cast_spell` mana check, the spellcasting menu display, and the two mana deductions in the player's cast states (looping and default). The filtered value is `SPELLS[spell].cost`. ctx is the spell id. Return the replacement cost, or `undefined` to keep the current value.

One hook, four dispatch sites: return the same cost for a given spell at every site, or the menu will display a price that disagrees with what the cast actually deducts.

| | |
| --- | --- |
| **Fires** | At all four engine reads of a spell's mana cost: the can-cast check, the menu display, and both cast-state deductions. |
| **Value** | `SPELLS[spell].cost`, the spell's mana cost. |
| **ctx** | The spell id. |
| **Kind contract** | The callback receives the current value and returns a replacement, or `undefined` to keep the current value. |

### The ctx parameter

- The spell id whose cost is being read.

## Usage

```gml
// spells.cost is a FILTER: you receive (value, ctx) and return a
// replacement, or undefined to keep the game's value.
function mana_miser_spells_cost(_value, _ctx) {
    // _value is SPELLS[spell].cost, the spell's mana cost.
    // _ctx is the spell id whose cost is being read.
    // Fires at four sites (check, menu, both deductions) - return the
    // same cost everywhere so the display matches the deduction.
    if (_value == undefined) return undefined; // test undefined BEFORE anything else
    return _value * 0.5; // half price: check, menu, and both deductions agree
}

mmapi_filter("spells.cost", mana_miser_spells_cost);
```

## Engine Wiring

- Seam [`spells_cost_can_cast`](../seams/spells_cost_can_cast.md) dispatches from `gml/scripts/Spells.gml`, in `can_cast_spell()`'s mana check (`ARI.get_mana() < cost`).
- Seam [`spells_cost_menu`](../seams/spells_cost_menu.md) dispatches from `gml/scripts/UI/Anchor/Menus/SpellcastingMenu.gml`, at the spell card's cost display. The menu shows the filtered cost `div 4`.
- Seam [`spells_cost_fsm_loop`](../seams/spells_cost_fsm_loop.md) dispatches from `gml/scripts/Player/AriFsm.gml`, at the mana deduction in the player's looping cast state (skipped while `MIST.is_running()`).
- Seam [`spells_cost_fsm_default`](../seams/spells_cost_fsm_default.md) dispatches from `gml/scripts/Player/AriFsm.gml`, at the mana deduction in the default cast state, right after `cast_spell()`.

## See Also

- [spells.can_cast](spells.can_cast.md) - Take over the whole can-cast decision instead of just its mana term.
- [spells.cast](spells.cast.md) - Replace the cast itself. A consumed cast still pays this cost.
- [player.mana_delta](player.mana_delta.md) - Filter the mana deduction itself, after this cost filter has run.
- [ui.sprite](ui.sprite.md) - Swap the spell card backplate in the same menu.
