# Hook: clock.time_advance

Adjust or freeze how much game time passes each frame.

`clock.time_advance` is a **filter** hook. Register a callback with `mmapi_filter`. See [Hooks](../HOOKS.md) for how registration and dispatch work.

## Contract

Fires inside `Clock.update()` each frame, after the natural per-frame time advance is computed (`GAME_SECONDS_PER_FRAME`, already gated by `game_paused()` and the clock's `time_stopped`) and before it is buffered into `side_buffer`. The filtered value is the game-seconds the clock is about to advance this frame. ctx is the `Clock`.

Return `0` to freeze the advance this frame (for example while the player is indoors), a replacement amount, or `undefined` to keep the natural advance. Suppressing the advance here does not touch `time_stopped`, so it composes with the engine's own freezes (end-of-day menu, cutscenes, challenge floors) instead of fighting them.

| | |
| --- | --- |
| **Fires** | Inside `Clock.update()` each frame, after the natural advance is computed and before it is buffered into `side_buffer`. |
| **Value** | The game-seconds the clock is about to advance this frame - `GAME_SECONDS_PER_FRAME`, already zeroed by `game_paused()` and `time_stopped`. |
| **ctx** | The `Clock` struct. |
| **Kind contract** | The callback receives the current value and returns a replacement, or `undefined` to keep the current value. |

### The ctx parameter

- ctx - the engine's `Clock` struct itself, the object whose `update()` is running. Read its state (e.g. `ctx.time_stopped`) to see how the natural value was gated. Do not set `time_stopped` from here. Return `0` instead.

## Usage

```gml
// clock.time_advance is a FILTER: you receive (value, ctx) and return a
// replacement, or undefined to keep the game's value.
function slow_days_clock_time_advance(_value, _ctx) {
    // _value is the game-seconds the clock advances this frame. It is
    //   already 0 while the game is paused or time is stopped.
    // _ctx is the Clock struct.
    if (_value == undefined) return undefined; // test undefined BEFORE anything else
    // freeze time while the player is indoors:
    // if (<player is indoors>) return 0;
    // or stretch every day to double length:
    // return _value * 0.5;
    return undefined; // undefined = keep the game's value
}

mmapi_filter("clock.time_advance", slow_days_clock_time_advance);
```

## Engine Wiring

- Seam [`clock_time_advance`](../seams/clock_time_advance.md) dispatches from `gml/scripts/GameplaySystems/TimeSeasons/Clock.gml`, rewriting the `side_buffer += GAME_SECONDS_PER_FRAME * ...` line inside `update()` so the computed advance passes through the filter before it is buffered.

## See Also

- [game.clock_tick](game.clock_tick.md) - This is the every-frame event at the top of the same `update()`. It fires even while paused or time-stopped, where this filter's natural value is already `0`.
- [game.day_started](game.day_started.md) - This is the day boundary that accumulated game time eventually crosses.
