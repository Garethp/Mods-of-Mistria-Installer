# Hook: player.equipment_bonus

Adjust the bonus an equipment infusion grants the player.

`player.equipment_bonus` is a **filter** hook. Register a callback with `mmapi_filter`. See [Hooks](../HOOKS.md) for how registration and dispatch work.

## Contract

Fires at the return of the player's equipment bonus lookup. The filtered value is the computed bonus. ctx is `{ player, infusion, key }`. Return the replacement value, or `undefined` to keep the current value.

| | |
| --- | --- |
| **Fires** | At the `return` of the player's equipment bonus lookup in `Ari`. |
| **Value** | The computed bonus the lookup is about to return. |
| **ctx** | `{ player, infusion, key }` |
| **Kind contract** | The callback receives the current value and returns a replacement, or `undefined` to keep the current value. |

### The ctx struct

- `player` - the `Ari` struct the lookup belongs to.
- `infusion` - the infusion whose bonus is being read, exactly as the engine passed it to the lookup.
- `key` - the bonus key being looked up for that infusion.

## Usage

```gml
// player.equipment_bonus is a FILTER: you receive (value, ctx) and return a
// replacement, or undefined to keep the game's value.
function gilded_gear_player_equipment_bonus(_value, _ctx) {
    // _value is the computed bonus the lookup is about to return.
    // _ctx is { player, infusion, key }.
    //   .player   - the Ari struct.
    //   .infusion - the infusion whose bonus is being read.
    //   .key      - the bonus key being looked up.
    if (_value == undefined) return undefined; // test undefined BEFORE anything else
    // if (_ctx.key == <the key you own>) return _value + 1; // sweeten one bonus
    return undefined; // undefined = keep the game's value
}

mmapi_filter("player.equipment_bonus", gilded_gear_player_equipment_bonus);
```

## Engine Wiring

- Seam [`player_equipment_bonus`](../seams/player_equipment_bonus.md) dispatches from `gml/scripts/GameplaySystems/Player/Ari.gml`, rewriting the lookup's `return value;` (the function directly above `get_damage_mitigation()`) to filter the computed bonus on the way out.

## See Also

- [player.incoming_damage](player.incoming_damage.md) - Change the final damage a hit deals the player.
- [player.move_speed](player.move_speed.md) - Change the player's move speed after every engine modifier.
- [player.max_health_item](player.max_health_item.md) - Know when an item permanently raises Ari's max health.
