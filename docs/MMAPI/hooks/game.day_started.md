# Hook: game.day_started

Know the moment a new day has begun.

`game.day_started` is an **event** hook. Register a callback with `mmapi_on`. See [Hooks](../HOOKS.md) for how registration and dispatch work.

## Contract

Fires from the begin_step derived-events poll when `total_days()` changes. ctx is `{ total_days }`. Observation only: the day has already started, and the first poll of a session only records the baseline.

| | |
| --- | --- |
| **Fires** | From the begin_step derived-events poll, when `total_days()` changes. |
| **ctx** | `{ total_days }` |
| **Kind contract** | The callback observes the moment. Its return value is ignored. |

### The ctx struct

- `total_days` - the new `total_days()` value, read the frame the poll saw it change.

> [!NOTE]
> The first poll of a session only records the current `total_days()` as the baseline. No event fires for the day the session starts in. After that, one event fires each time the value changes.

## Usage

```gml
// game.day_started is an EVENT: mmapi calls you after it happens.
// You cannot change or stop it here; the return value is ignored.
function morning_briefing_game_day_started(_ctx) {
    // _ctx is { total_days }.
    //   .total_days - the new total_days() value for the day that just began.
    // The day has already started when this fires.
    // your once-per-day code here
}

// inside your latched register function (see Mod Anatomy):
mmapi_on("game.day_started", morning_briefing_game_day_started);
```

## Engine Wiring

- This event is emitted by the mmapi framework itself. No engine seam sits behind it. `mmapi_events_poll()` in `mmapi\mmapi_events.gml` reads `total_days()` once per frame from the Game begin_step lifecycle drain (installed by the [`game_step_begin_installs`](../seams/game_step_begin_installs.md) engine fix) and emits when the value changes. The first poll only records the baseline.

## See Also

- [game.room_changed](game.room_changed.md) - This event is the other main derived event from the same poll.
- [game.clock_tick](game.clock_tick.md) - This is the every-frame clock event, for when a day is too coarse.
- [clock.time_advance](clock.time_advance.md) - Control how fast the day boundary approaches.
