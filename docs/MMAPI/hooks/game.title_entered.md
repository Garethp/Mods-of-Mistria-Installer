# Hook: game.title_entered

Know when the game returns to the title screen.

`game.title_entered` is an **event** hook. Register a callback with `mmapi_on`. See [Hooks](../HOOKS.md) for how registration and dispatch work.

## Contract

Fires from the begin_step derived-events poll when the room changes into the title/menu room from a non-title room. ctx is `{}` (empty struct). This hook is observation only, and it fires after [game.room_changed](game.room_changed.md) for the same room change.

| | |
| --- | --- |
| **Fires** | From the begin_step derived-events poll, when the room changes into the title/menu room from a non-title room. |
| **ctx** | `{}` (empty struct) |
| **Kind contract** | The callback observes the moment. Its return value is ignored. |

### The ctx struct

- The ctx is an empty struct: the moment carries no data. The title room itself is `rm_menu`, the engine's title and menu room (per `is_menu_room` in `RoomCheckScripts.gml`).

> [!NOTE]
> This is an edge event: it fires only on the change from a non-title room into the title room, so re-entering menus while already on the title screen does not re-fire it. Because begin_step runs after `room_start`, the title room is already up when your handler runs. Use it to tear down per-session state. A save that was in play is over.

## Usage

```gml
// game.title_entered is an EVENT: mmapi calls you after it happens.
// You cannot change or stop it here; the return value is ignored.
function session_stats_game_title_entered(_ctx) {
    // _ctx is {} - an empty struct, the moment carries no data.
    // The game has left a play session and is back on the title screen:
    // reset any per-save state your mod holds.
}

// inside your latched register function (see Mod Anatomy):
mmapi_on("game.title_entered", session_stats_game_title_entered);
```

## Engine Wiring

- This event is emitted by the mmapi framework itself. No engine seam sits behind it. `mmapi_events_poll()` in `mmapi\mmapi_events.gml` runs once per frame from the Game begin_step lifecycle drain (installed by the [`game_step_begin_installs`](../seams/game_step_begin_installs.md) engine fix). When a room change lands in the title room and the previous room was not the title, it emits this event right after `game.room_changed`. The first poll of a session only records the baseline.

## See Also

- [game.room_changed](game.room_changed.md) - This is the general room-change event from the same poll. It fires first for the same change.
- [save.game_loaded](save.game_loaded.md) - This is the matching start-of-session signal, which fires when a save begins loading.
