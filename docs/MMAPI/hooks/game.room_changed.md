# Hook: game.room_changed

Know when the player has landed in a different room.

`game.room_changed` is an **event** hook. Register a callback with `mmapi_on`. See [Hooks](../HOOKS.md) for how registration and dispatch work.

## Contract

Fires from the begin_step derived-events poll when `room()` changes, after `room_start` has already run. ctx is `{ previous, current }` (`gm_room` values). Observation only: to change room content as it loads, use the in-file seams ([dungeon.floor_enter](dungeon.floor_enter.md) and the room transition events) instead.

| | |
| --- | --- |
| **Fires** | From the begin_step derived-events poll, when `room()` changes, after `room_start` has already run. |
| **ctx** | `{ previous, current }` |
| **Kind contract** | The callback observes the moment. Its return value is ignored. |

### The ctx struct

- `previous` - the `gm_room` the game was in on the last poll (the room that was left).
- `current` - the `gm_room` the game is in now, the value `room()` returned this frame.

> [!NOTE]
> This event fires after the fact: begin_step runs after `room_start`, so the new room is already set up when your handler runs. The first poll of a session only records the current room as the baseline. No event fires for the room the session starts in.

## Usage

```gml
// game.room_changed is an EVENT: the return value is ignored.
// You cannot change or stop it here; the return value is ignored.
function travel_journal_game_room_changed(_ctx) {
    // _ctx is { previous, current }.
    //   .previous - the gm_room that was left.
    //   .current  - the gm_room the game is in now (room_start already ran).
    // your code here
}

// inside your latched register function (see Mod Anatomy):
mmapi_on("game.room_changed", travel_journal_game_room_changed);
```

## Engine Wiring

- This event is emitted by the MMAPI framework itself. No engine seam sits behind it. `mmapi_events_poll()` in `mmapi/mmapi_events.gml` compares `room()` against the last poll once per frame from the Game begin_step lifecycle drain (installed by the [`game_step_begin_installs`](../seams/game_step_begin_installs.md) engine fix) and emits on a change. The first poll only records the baseline.

## See Also

- [game.room_transition_pre](game.room_transition_pre.md) - This hook fires before a taxi transition starts, and it can redirect it.
- [game.room_transition_post](game.room_transition_post.md) - This hook marks the end of the taxi transition, after arrival and music selection.
- [dungeon.floor_enter](dungeon.floor_enter.md) - This is the in-file alternative for changing dungeon room content as it loads.
- [game.title_entered](game.title_entered.md) - This is the derived event for the specific change into the title room. It fires after this event for the same room change.
