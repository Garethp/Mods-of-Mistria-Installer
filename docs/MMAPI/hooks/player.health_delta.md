# Hook: player.health_delta

Change every player health gain or loss before it lands.

`player.health_delta` is a **filter** hook. Register a callback with `mmapi_filter`. See [Hooks](../HOOKS.md) for how registration and dispatch work.

## Contract

Fires at the top of `Ari.modify_health()`, before the delta is applied. The filtered value is the signed health delta. ctx is `{ player, play_sound }`. Return the replacement delta (`0` makes the call a no-op: `modify_health` early-returns before touching health), or `undefined` to keep the current value.

Combat hits arrive here too: the player's damage drain calls `modify_health` with the final damage after [player.incoming_damage](player.incoming_damage.md) has run, so this filter sees combat damage last.

| | |
| --- | --- |
| **Fires** | At the top of `Ari.modify_health(amount_to_add, play_sound)`, before the delta is applied. |
| **Value** | The signed health delta: negative hurts, positive heals. |
| **ctx** | `{ player, play_sound }` |
| **Kind contract** | The callback receives the current value and returns a replacement, or `undefined` to keep the current value. |

### The ctx struct

- `player` - the `Ari` struct whose health is changing.
- `play_sound` - the `play_sound` flag `modify_health` received: whether the engine intends to play the accompanying sound for this change.

## Usage

```gml
// player.health_delta is a FILTER: you receive (value, ctx) and return a
// replacement, or undefined to keep the game's value.
function iron_constitution_player_health_delta(_value, _ctx) {
    // _value is the signed health delta: negative = damage, positive = heal.
    // _ctx is { player, play_sound }.
    //   .player     - the Ari struct whose health is changing.
    //   .play_sound - whether modify_health was asked to play its sound.
    if (_value == undefined) return undefined; // test undefined BEFORE anything else
    if (_value < 0) return 0; // 0 makes modify_health a no-op: no damage sticks
    return undefined; // undefined = keep the game's value
}

mmapi_filter("player.health_delta", iron_constitution_player_health_delta);
```

## Engine Wiring

- Seam [`player_health_delta`](../seams/player_health_delta.md) dispatches from `gml/scripts/GameplaySystems/Player/Ari.gml`, at the head of `modify_health(amount_to_add, play_sound)`, filtering `amount_to_add` before the engine's zero-check and `set_health` run.

## See Also

- [player.incoming_damage](player.incoming_damage.md) - Filter combat damage specifically, before it reaches `modify_health`.
- [player.stamina_delta](player.stamina_delta.md) - This is the same filter point for stamina.
- [player.essence_delta](player.essence_delta.md) - The same filter family: essence, gold, mana, and skill XP each have theirs.
- [player.heal_vfx](player.heal_vfx.md) - Veto the heal sparkle without touching the heal.
