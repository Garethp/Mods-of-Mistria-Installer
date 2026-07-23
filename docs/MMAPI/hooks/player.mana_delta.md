# Hook: player.mana_delta

Change every mana gain or spend before it applies.

`player.mana_delta` is a **filter** hook. Register a callback with `mmapi_filter`. See [Hooks](../HOOKS.md) for how registration and dispatch work.

## Contract

Fires at the top of `Ari.modify_mana()`, before the delta is applied. The filtered value is the signed mana delta. The ctx is `{ player }`. Return the replacement delta (`0` makes the call a no-op), or `undefined` to keep the current value.

Item restores, such as the mana potion, fire this hook too. Cutscenes and other absolute sets do not.

> [!TIP]
> Composes with [spells.cost](spells.cost.md): a cast's deduction arrives here as the negative of the already-filtered cost. Prefer `spells.cost` for spell pricing. It keeps the menu display, the can-cast check, and the deduction in agreement. Use this hook for everything else.

| | |
| --- | --- |
| **Fires** | At the top of `Ari.modify_mana(amount_to_add)`, before the delta is applied. |
| **Value** | The signed mana delta: deductions negative, restores positive. |
| **ctx** | `{ player }` |
| **Kind contract** | The callback receives the current value and returns a replacement, or `undefined` to keep the current value. |

### The ctx struct

- `player` - the `Ari` struct whose mana is changing.

## Usage

```gml
// player.mana_delta is a FILTER: you receive (value, ctx) and return a
// replacement, or undefined to keep the game's value.
function overcharged_player_mana_delta(_value, _ctx) {
    // _value is the signed mana delta: negative = deduction, positive = restore.
    // Cast deductions arrive as the negative of the (spells.cost-filtered) cost;
    // prefer spells.cost for pricing so the menu and deduction agree.
    // _ctx is { player }.
    //   .player - the Ari struct whose mana is changing.
    if (_value == undefined) return undefined; // test undefined BEFORE anything else
    if (_value > 0) return _value * 2; // double every restore (set_mana clamps at mana_max)
    return undefined; // undefined = keep deductions unchanged
}

mmapi_filter("player.mana_delta", overcharged_player_mana_delta);
```

## Engine Wiring

- Seam [`player_mana_delta`](../seams/player_mana_delta.md) dispatches from `gml/scripts/GameplaySystems/Player/Ari.gml`, at the head of `modify_mana(amount_to_add)`, filtering `amount_to_add` before the engine's `set_mana` clamps and applies the total.
- Seam [`player_mana_item_delta`](../seams/player_mana_item_delta.md) dispatches nothing itself: it reroutes the mana potion's direct `set_mana` call in `gml/scripts/Player/AriFsm.gml` through `modify_mana`, so item restores reach the filter above.

## See Also

- [spells.cost](spells.cost.md) - Change a spell's price instead of the deduction it produces.
- [player.essence_delta](player.essence_delta.md) - The similar filter point for essence.
- [player.stamina_delta](player.stamina_delta.md) - The similar filter point for stamina.
