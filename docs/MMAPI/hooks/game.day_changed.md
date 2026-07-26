# Hook: game.day_changed

Know when the current day has changed - a new day beginning, or a save load landing in a different one.

`game.day_changed` is an **event** hook. Register a callback with `mmapi_on`. See [Hooks](../HOOKS.md) for how registration and dispatch work.

## Contract

Fires from the begin_step derived-events poll when `total_days()` changes. ctx is `{ total_days }`. Observation only: the change has already happened, and the first poll of a session only records the baseline.

| | |
| --- | --- |
| **Fires** | From the begin_step derived-events poll, when `total_days()` changes. |
| **ctx** | `{ total_days }` |
| **Kind contract** | The callback observes the moment. Its return value is ignored. |

### The ctx struct

- `total_days` - the new `total_days()` value, read the frame the poll saw it change.

> [!NOTE]
> The first poll of a session only records the current `total_days()` as the baseline. No event fires for the day the session starts in. After that, one event fires each time the value changes.

> [!NOTE]
> "The value changes" is the whole contract. A cross-day save load changes `total_days()` too, so this event can fire right after loading a save from a different day - and because the poll runs a frame after the end-of-day sequence, a real overnight fires it after the end-of-day autosave has already written. For the engine's new-day logic itself - fired inside `new_day()` before that autosave, and never on a save load - see [game.new_day](game.new_day.md).

> [!NOTE]
> `game.day_started` is this hook's old name. A registration against the old name still resolves here, with a one-time warning per mod in the MMAPI log. Update the registration to `game.day_changed`.

## Usage

```gml
// game.day_changed is an EVENT: the return value is ignored.
// You cannot change or stop it here; the return value is ignored.
function date_display_game_day_changed(_ctx) {
    // _ctx is { total_days }.
    //   .total_days - the new total_days() value the poll observed.
    // The session is in a different day than it was - an overnight, or a
    // cross-day save load. Refresh anything derived from the current date.
    // Daily resets and grants belong on game.new_day instead.
}

// inside your latched register function (see Mod Anatomy):
mmapi_on("game.day_changed", date_display_game_day_changed);
```

## Engine Wiring

- This event is emitted by the MMAPI framework itself. No engine seam sits behind it. `mmapi_events_poll()` in `mmapi/mmapi_events.gml` reads `total_days()` once per frame from the Game begin_step lifecycle drain (installed by the [`game_step_begin_installs`](../seams/game_step_begin_installs.md) engine fix) and emits when the value changes. The first poll only records the baseline.

## See Also

- [game.new_day](game.new_day.md) - This is the engine's new-day logic itself: fired inside `new_day()` before the end-of-day autosave, never after a save load. Prefer it for daily resets and grants.
- [game.room_changed](game.room_changed.md) - This event is the other main derived event from the same poll.
- [game.clock_tick](game.clock_tick.md) - This is the every-frame clock event, for when a day is too coarse.
- [clock.time_advance](clock.time_advance.md) - Control how fast the day boundary approaches.
