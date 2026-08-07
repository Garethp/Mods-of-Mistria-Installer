# Hook: player.mount_speed

Change the mounted base speed before the shared status multipliers.

`player.mount_speed` is a **filter** hook. Register a callback with `mmapi_filter`. See [Hooks](../HOOKS.md) for how registration and dispatch work.

## Contract

Fires in the player's move speed computation, only when the speed is being computed for mounted movement, right after the mounted base speed is chosen and before the shared status effect multipliers (`StatusEffectId.MineTime`, `SlimeDash`, `KillHaste`) and the final [player.move_speed](player.move_speed.md) filter. Never fires on foot. The filtered value is the mounted base speed: `MOUNT_WALK_SPEED` when walking, and `MOUNT_RUN_SPEED` (or `MOUNT_HORSEPOWER_RUN_SPEED` with the Horsepower perk) when running. ctx is `{ player }`. Return the replacement base speed, or `undefined` to keep the current value.

| | |
| --- | --- |
| **Fires** | Inside `Ari.get_move_speed(on_mount)`, in an `if on_mount` block after the base speed branch and before the status-effect multipliers. |
| **Value** | The mounted base speed, Horsepower perk already applied. |
| **ctx** | `{ player }` |
| **Kind contract** | The callback receives the current value and returns a replacement, or `undefined` to keep the current value. |

### The ctx struct

- `player` - the `Ari` struct whose speed is being computed. Read `ctx.player.run_toggle` to tell walk from run, and `ctx.player.mount` for the mount's Animal struct. The mount's kind and variant are cosmetic only, since the engine never varies mount speed by mount.

## Usage

```gml
// player.mount_speed is a FILTER: you receive (value, ctx) and return a
// replacement, or undefined to keep the game's value.
function destrier_player_mount_speed(_value, _ctx) {
    // _value is the mounted BASE speed. The status multipliers apply after you.
    // _ctx is { player }.
    //   .player            - the Ari struct.
    //   .player.run_toggle - true for the run gait, false for the walk.
    //   .player.mount      - the mount's Animal struct (cosmetic identity).
    if (_value == undefined) return undefined; // test undefined BEFORE anything else
    if (!_ctx.player.run_toggle) return undefined; // leave the walk alone
    return 5.0; // an absolute run base for a custom steed. MineTime and
                // friends still multiply on top, exactly like vanilla.
}

mmapi_filter("player.mount_speed", destrier_player_mount_speed);
```

Prefer this hook to give a mount an absolute base speed that still composes with the engine's status multipliers. For a multiplicative tweak to the final mounted speed ("the mount is 30% faster"), prefer [player.move_speed](player.move_speed.md) and act when `ctx.on_mount` is true. The two compose, and `player.move_speed` runs later.

## Engine Wiring

- Seam [`player_mount_speed`](../seams/player_mount_speed.md) dispatches from `gml/scripts/GameplaySystems/Player/Ari.gml`, filtering `spd` in an `if on_mount` block between the base-speed branch and the `MineTime`/`SlimeDash`/`KillHaste` status effect multipliers.

## See Also

- [player.move_speed](player.move_speed.md) - Change the final speed after every engine modifier, mounted or on foot.
- [player.swim_speed](player.swim_speed.md) - Change the swim speed the swim states read instead of this computation.
- [player.status_effect_register](player.status_effect_register.md) - Rewrite the status effects whose multipliers apply after this hook.
