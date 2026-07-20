# Seam: clock_time_advance

Routes each frame's game-seconds advance through the filter chain before it reaches the clock's buffer.

`clock_time_advance` is a **text seam** (`anchor` + `replace`). It feeds [clock.time_advance](../hooks/clock.time_advance.md). Mod authors never write seams. You register handlers for the hooks they dispatch. See [Seams](../SEAMS.md).

## Placement

| | |
| --- | --- |
| **File** | `gml/scripts/GameplaySystems/TimeSeasons/Clock.gml` |
| **Locator** | text anchor inside `Clock.update()`: the one-line `side_buffer` advance |
| **Feeds** | [`clock.time_advance`](../hooks/clock.time_advance.md) |
| **Value filtered** | the game-seconds the clock is about to advance this frame |
| **ctx built** | `self` - the `Clock` struct |
| **Marker** | `mmapi_clock_run_time_advance` |

## The Edit

Pristine `Clock.update()` buffers the frame's advance in one line: `self.side_buffer += GAME_SECONDS_PER_FRAME * !game_paused() * !self.time_stopped;`. The seam splits that line into capture, filter, buffer. The natural advance (already multiplied to zero by `game_paused()` and the clock's own `time_stopped`) is captured into `__clock_advance`, passed through `mmapi_apply_filters("clock.time_advance", __clock_advance, self)`, and the filtered result is what reaches `side_buffer`.

Because the filter runs after the engine's own gating, a handler that returns `0` freezes the advance for this frame without touching `time_stopped`. It composes with the engine's own freezes (end-of-day menu, cutscenes, challenge floors) instead of fighting them. Handlers return a replacement amount to speed time up or slow it down, or `undefined` to keep the natural advance.

## See Also

- [clock.time_advance](../hooks/clock.time_advance.md) - This is the hook that this seam dispatches.
- [game_clock_tick](game_clock_tick.md) - This is the other seam in `Clock.gml`. It is the unconditional every-frame tick at the head of the same function.
