# Seam: new_day_complete

Announces the completion of the engine's new-day work, inside `new_day()` and ahead of the end-of-day autosave.

`new_day_complete` is a **template seam** (`op = "emit"`). It feeds [game.new_day](../hooks/game.new_day.md). Mod authors never write seams. You register handlers for the hooks they dispatch. See [Seams](../SEAMS.md).

## Placement

| | |
| --- | --- |
| **File** | `gml/scripts/GameplaySystems/Cycle/NewDay.gml` |
| **Locator** | structural target: `new_day`, after `new_day_grid();` |
| **Op** | `emit` |
| **Feeds** | [`game.new_day`](../hooks/game.new_day.md) |
| **ctx built** | `{ total_days: total_days() }` |
| **Marker** | `mmapi_run_new_day_callbacks` |

## The Edit

The generated emit is placed structurally inside `new_day`, immediately after `new_day_grid();` - the second of the two calls that do the day's transition work (`new_day_non_grid()` then `new_day_grid()`), so every system the engine rolls over for the new day has rolled before handlers run. On the sleep path the one gameplay caller is `EodMenu`, which calls `save_game()` for the end-of-day autosave right after `new_day()` returns - handler effects land in that save. It calls `mmapi_emit("game.new_day", { total_days: total_days() })` in the uniform try/catch shape (`catch_var = "__mmapi_new_day"`).

The engine's debug and test-suite `new_day()` callers route through the same function and fire the same emit. With zero handlers the seam is behaviorally identical to pristine.

## See Also

- [game.new_day](../hooks/game.new_day.md) - This is the hook this seam dispatches.
- [save_game_saving](save_game_saving.md) - This seam's emit is the next mod-visible moment on the end-of-day path, when the autosave commits.
- [game_step_begin_installs](game_step_begin_installs.md) - This engine fix drives the poll that derives [game.day_changed](../hooks/game.day_changed.md), the observation-side counterpart.
- [player_pass_out](player_pass_out.md) - This is the emit inside `pass_out()`, right after `end_day()`.
