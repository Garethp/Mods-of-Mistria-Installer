# Seam: game_clock_tick

Emits the every-frame clock tick from the head of `Clock.update()`.

`game_clock_tick` is a **template seam** (`op = "emit"`). It feeds [game.clock_tick](../hooks/game.clock_tick.md). Mod authors never write seams. You register handlers for the hooks they dispatch. See [Seams](../SEAMS.md).

## Placement

| | |
| --- | --- |
| **File** | `gml/scripts/GameplaySystems/TimeSeasons/Clock.gml` |
| **Locator** | structural target: `update`, at head |
| **Op** | `emit` |
| **Feeds** | [`game.clock_tick`](../hooks/game.clock_tick.md) |
| **ctx built** | `self` - the `Clock` struct |
| **Marker** | `mmapi_game_run_clock_tick` |

## The Edit

The generated dispatch lands at the head of `Clock.update()`, before the engine buffers any game seconds. It calls `mmapi_emit("game.clock_tick", self)` in the uniform try/catch shape, handing handlers the live `Clock` struct once per frame.

`update()` runs every frame regardless of game state (the pause and time-stop gating happens later in the function, where the advance is computed), so the hook fires even while the game is paused or time is stopped. With zero handlers the seam is behaviorally identical to pristine: the emit early-outs on an empty registry.

## See Also

- [game.clock_tick](../hooks/game.clock_tick.md) - This is the hook this seam dispatches.
- [clock_time_advance](clock_time_advance.md) - This is the other seam in `Clock.gml`. It filters the per-frame time advance that this tick precedes.
