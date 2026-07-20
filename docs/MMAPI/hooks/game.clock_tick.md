# Hook: game.clock_tick

Know every frame the game clock ticks, even while paused.

`game.clock_tick` is an **event** hook. Register a callback with `mmapi_on`. See [Hooks](../HOOKS.md) for how registration and dispatch work.

## Contract

Fires at the top of `Clock.update()` every frame, before game seconds are buffered. ctx is the `Clock` struct. Fires even while the game is paused or time is stopped.

| | |
| --- | --- |
| **Fires** | At the top of `Clock.update()`, every frame, before game seconds are buffered. |
| **ctx** | The `Clock` struct. |
| **Kind contract** | The callback observes the moment. Its return value is ignored. |

### The ctx parameter

- ctx - the engine's `Clock` struct itself, the object whose `update()` is running. Read the clock's state directly from it (e.g. `ctx.time_stopped`).

> [!NOTE]
> This is the framework's per-frame heartbeat: it fires every frame regardless of `game_paused()` or the clock's `time_stopped`, so it is the place for work that must keep running while the game world is frozen. Keep the handler's first check its cheapest early-exit.

## Usage

```gml
// game.clock_tick is an EVENT: the return value is ignored.
// You cannot change or stop it here; the return value is ignored.
function clock_watcher_game_clock_tick(_ctx) {
    // _ctx is the Clock struct whose update() is running.
    //   read clock state from it directly, e.g. _ctx.time_stopped.
    // Fires every frame, even while paused or time-stopped. Make your first
    // check the cheapest one and get out early.
    if (!__clock_watcher_runtime().enabled) return;
    // your per-frame code here
}

// inside your latched register function (see Mod Anatomy):
mmapi_on("game.clock_tick", clock_watcher_game_clock_tick);
```

## Engine Wiring

- Seam [`game_clock_tick`](../seams/game_clock_tick.md) dispatches from `gml/scripts/GameplaySystems/TimeSeasons/Clock.gml`, an emit at the head of `update()` with the clock itself as ctx.

## See Also

- [clock.time_advance](clock.time_advance.md) - This is the filter on the per-frame game-seconds advance that `update()` buffers right after this event. Unlike `game.clock_tick`, its natural value is already gated to `0` while paused or time-stopped.
- [game.day_started](game.day_started.md) - This is the day-boundary event, if you only care when `total_days()` changes.
