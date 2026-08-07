# Hook: player.move_speed

Change the player's move speed after every engine modifier.

`player.move_speed` is a **filter** hook. Register a callback with `mmapi_filter`. See [Hooks](../HOOKS.md) for how registration and dispatch work.

## Contract

Fires at the end of the player's move speed computation, after the status effect multipliers (`StatusEffectId.MineTime`, `SlimeDash`, `KillHaste`). The filtered value is the speed. ctx is `{ player, on_mount }`. Return the replacement speed, or `undefined` to keep the current value. Your handler runs last, so what you return is what the engine returns. Mounted movement routes through this computation too, with `ctx.on_mount` set true, and the mounted base speed is separately filterable earlier in the computation via [player.mount_speed](player.mount_speed.md). Swimming does not route through here. That path is [player.swim_speed](player.swim_speed.md).

| | |
| --- | --- |
| **Fires** | At the end of the player's move speed computation, after the status effect multipliers and before `return spd;`. |
| **Value** | The computed move speed. |
| **ctx** | `{ player, on_mount }` |
| **Kind contract** | The callback receives the current value and returns a replacement, or `undefined` to keep the current value. |

### The ctx struct

- `player` - the `Ari` struct whose speed is being computed.
- `on_mount` - whether the speed is being computed for mounted movement, as passed into the computation.

## Usage

```gml
// player.move_speed is a FILTER: you receive (value, ctx) and return a
// replacement, or undefined to keep the game's value.
function swift_boots_player_move_speed(_value, _ctx) {
    // _value is the computed move speed, all engine modifiers applied.
    // _ctx is { player, on_mount }.
    //   .player   - the Ari struct.
    //   .on_mount - true when the speed is for mounted movement.
    if (_value == undefined) return undefined; // test undefined BEFORE anything else
    if (_ctx.on_mount) return undefined; // leave mount speed alone
    return _value * 1.25; // a permanent 25% spring in Ari's step
}

mmapi_filter("player.move_speed", swift_boots_player_move_speed);
```

## Engine Wiring

- Seam [`player_move_speed`](../seams/player_move_speed.md) dispatches from `gml/scripts/GameplaySystems/Player/Ari.gml`, filtering `spd` after the `MineTime`/`SlimeDash`/`KillHaste` status effect multipliers and immediately before `return spd;`.

## See Also

- [player.mount_speed](player.mount_speed.md) - Change the mounted base speed before the shared status multipliers.
- [player.swim_speed](player.swim_speed.md) - Change the swim speed the swim states read instead of this computation.
- [player.status_effect_register](player.status_effect_register.md) - Rewrite the status effects whose multipliers feed this computation.
- [player.stamina_delta](player.stamina_delta.md) - Change every stamina cost or gain before it applies.
- [player.equipment_bonus](player.equipment_bonus.md) - Adjust the bonus an equipment infusion grants the player.
