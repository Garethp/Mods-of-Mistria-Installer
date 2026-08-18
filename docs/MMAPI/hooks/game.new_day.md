# Hook: game.new_day

Know the moment the engine's new-day logic has run, before the end-of-day autosave.

`game.new_day` is an **event** hook. Register a callback with `mmapi_on`. See [Hooks](../HOOKS.md) for how registration and dispatch work.

## Contract

Fires from inside `new_day()` (Cycle/NewDay.gml), after the engine's day-transition work completes. On the end-of-day path that is before the autosave writes. ctx is `{ total_days }`.

| | |
| --- | --- |
| **Fires** | Inside `new_day()`, after `new_day_grid()` completes the day-transition work. |
| **ctx** | `{ total_days }` |
| **Kind contract** | The callback observes the moment. Its return value is ignored. |

### The ctx struct

- `total_days` - the new `total_days()` value for the day that just began.

> [!NOTE]
> This is the day-boundary event whose effects land in the end-of-day autosave. The one gameplay caller of `new_day()` is `EodMenu`'s sleep sequence, which calls `save_game()` immediately after `new_day()` returns - so state a handler writes here (mod-save fields, mana, gold) is captured by that save. The poll-derived [game.day_changed](game.day_changed.md) fires one frame later, after the autosave has already written.

> [!NOTE]
> This event never fires on a save load, and it also fires from the engine's own debug and test-suite `new_day()` callers - a skipped day is still a day. Handlers run inside the end-of-day call stack, not at a frame boundary: keep them to memory and state work.

## Usage

```gml
// game.new_day is an EVENT: the return value is ignored.
// You cannot change or stop it here; the return value is ignored.
function daily_reset_game_new_day(_ctx) {
    // _ctx is { total_days }.
    //   .total_days - the new total_days() value for the day that just began.
    // The engine's day-transition work is complete; the end-of-day autosave has NOT run yet.
    // your once-per-day resets and grants here
}

// inside your latched register function (see Mod Anatomy):
mmapi_on("game.new_day", daily_reset_game_new_day);
```

## Engine Wiring

- Seam [`new_day_complete`](../seams/new_day_complete.md) dispatches from `gml/scripts/GameplaySystems/Cycle/NewDay.gml`, an emit inside `new_day()` right after `new_day_grid();`. `EodMenu`'s sleep sequence - the one gameplay caller - saves the end-of-day autosave immediately after `new_day()` returns, which is what orders this event before that save.

## See Also

- [game.day_changed](game.day_changed.md) - This is the poll-side observation of the day changing: it fires one frame later (after the end-of-day autosave), also fires after a cross-day save load, and never fires for the day a session starts in. Prefer `game.new_day` for daily resets and grants.
- [save.game_saving](save.game_saving.md) - This fires next on the end-of-day path, when the autosave commits.
- [game.clock_tick](game.clock_tick.md) - This is the every-frame clock event, for when a day is too coarse.
- [player.pass_out](player.pass_out.md) - This event fires inside `pass_out()`, right after the `end_day()` call that starts this rollover.
