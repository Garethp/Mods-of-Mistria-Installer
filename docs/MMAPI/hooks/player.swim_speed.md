# Hook: player.swim_speed

Change the player's swim speed after every engine modifier.

`player.swim_speed` is a **filter** hook. Register a callback with `mmapi_filter`. See [Hooks](../HOOKS.md) for how registration and dispatch work.

## Contract

Fires at the end of `Ari.get_swim_speed()`, after the fast/slow stroke branch. The filtered value is the swim speed. With the run toggle held it is `HUMAN_SWIM_FAST` with the Hasty infusion and the Speedy status effect folded in. Otherwise it is bare `HUMAN_SWIM_SLOW`, since the slow drift gets no engine modifiers at all. ctx is `{ player }`. Return the replacement speed, or `undefined` to keep the current value. Your handler runs last, so what you return is what the engine returns.

| | |
| --- | --- |
| **Fires** | At the end of `Ari.get_swim_speed()`, immediately before `return spd;`. Every swimming frame, so keep handlers cheap. |
| **Value** | The computed swim speed, all engine modifiers applied. |
| **ctx** | `{ player }` |
| **Kind contract** | The callback receives the current value and returns a replacement, or `undefined` to keep the current value. |

### The ctx struct

- `player` - the `Ari` struct whose speed is being computed. Read `ctx.player.run_toggle` to tell the fast stroke from the slow drift.

## Usage

```gml
// player.swim_speed is a FILTER: you receive (value, ctx) and return a
// replacement, or undefined to keep the game's value.
function otter_player_swim_speed(_value, _ctx) {
    // _value is the computed swim speed, all engine modifiers applied.
    // _ctx is { player }.
    //   .player            - the Ari struct.
    //   .player.run_toggle - true for the fast stroke, false for the slow drift.
    if (_value == undefined) return undefined; // test undefined BEFORE anything else
    return _value * 1.5; // Ari swims like an otter
}

mmapi_filter("player.swim_speed", otter_player_swim_speed);
```

Both engine callers flow through the filter: the Swim state's per-frame movement, and the Underwater state's magnetic pull toward the dive point, which moves at half the filtered value. The hook is disjoint from [player.move_speed](player.move_speed.md) and [player.mount_speed](player.mount_speed.md), because the swim states never consult `get_move_speed`.

## Engine Wiring

- Seam [`player_swim_speed`](../seams/player_swim_speed.md) dispatches from `gml/scripts/GameplaySystems/Player/Ari.gml`, filtering `spd` immediately before `return spd;` in `get_swim_speed()`.

## See Also

- [player.move_speed](player.move_speed.md) - Change the player's move speed on foot and mounted.
- [player.mount_speed](player.mount_speed.md) - Change the mounted base speed before the shared status multipliers.
- [player.equipment_bonus](player.equipment_bonus.md) - Adjust the Hasty infusion bonus the fast stroke folds in.
