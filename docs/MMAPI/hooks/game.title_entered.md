# Hook: game.title_entered

Know when the title screen comes up, at boot or when a session ends.

`game.title_entered` is an **event** hook. Register a callback with `mmapi_on`. See [Hooks](../HOOKS.md) for how registration and dispatch work.

## Contract

Fires from the head of `TitleMenu.setup_main_screen()` on every title-screen entry: at boot, once the logo chain hands over to the main screen, and again whenever a play session quits back to the title. ctx is `{ from_game }`. This hook is observation only.

| | |
| --- | --- |
| **Fires** | From the head of `TitleMenu.setup_main_screen()`, on every title-screen entry. |
| **ctx** | `{ from_game }` |
| **Kind contract** | The callback observes the moment. Its return value is ignored. |

### The ctx struct

- `from_game` — `false` for the boot entry, `true` when a play session has just ended. This is the engine's own `FROM_GAME` flag, read while it is still set (it only resets inside `enter_game`).

> [!NOTE]
> The quit-to-title entry is the per-session teardown moment: a save that was in play is over, so reset any per-save state your mod holds. The boot entry fires before any session has existed, so a reset handler is harmless there — but gate on `ctx.from_game` if your teardown must only run when a session actually ended. Handlers run inside the title menu's build chain, before the player can interact with the menu.

## Usage

```gml
// game.title_entered is an EVENT: the return value is ignored.
// You cannot change or stop it here; the return value is ignored.
function session_stats_game_title_entered(ctx) {
    // A play session is over (or the game just booted): reset any
    // per-save state your mod holds.
    if (ctx.from_game != true) { return; }   // boot entry: nothing to tear down
    // ... clear latches, disarm pending work ...
}

// inside your latched register function (see Mod Anatomy):
mmapi_on("game.title_entered", session_stats_game_title_entered);
```

## Engine Wiring

- The [`title_menu_main_screen`](../seams/title_menu_main_screen.md) seam places the emit at the head of `TitleMenu.setup_main_screen()`, the one method both title entries funnel through — the same method the legacy YYTK mods hooked for this moment. An earlier version of this hook was runtime-provided, derived from the begin_step room poll; it could never fire, because no Game instance ever steps in the title room (at the boot title none exists yet, and quit-to-title halts stepping entirely).

## See Also

- [save.game_loaded](save.game_loaded.md) - This is the matching start-of-session signal, which fires when a save begins loading.
- [game.room_changed](game.room_changed.md) - This is the general room-change event from the begin_step poll. It never observes the title room, which is why this hook is seam-fed.
