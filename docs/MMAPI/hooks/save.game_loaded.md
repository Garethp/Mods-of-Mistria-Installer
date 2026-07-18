# Hook: save.game_loaded

Know the moment a save file starts loading.

`save.game_loaded` is an **event** hook. Register a callback with `mmapi_on`. See [Hooks](../HOOKS.md) for how registration and dispatch work.

## Contract

Fires at the start of loading a save, right after the save path is recorded. ctx is `{ save_path }`.

| | |
| --- | --- |
| **Fires** | At the start of loading a save, right after the save path is recorded. |
| **ctx** | `{ save_path }` |
| **Kind contract** | The callback observes the moment. Its return value is ignored. |

### The ctx struct

- `save_path` - the loader's `save_path`: the path of the save file being loaded, the same value the engine records as `Game.last_serde_path`.

> [!NOTE]
> This fires at the start of the load, not the end. The world is not built yet. Use it to key your mod's per-save state to the file being loaded. Wait for [game.room_changed](game.room_changed.md) or [game.day_started](game.day_started.md) style signals before touching world content.

## Usage

```gml
// save.game_loaded is an EVENT: the return value is ignored.
// You cannot change or stop it here; the return value is ignored.
function fresh_start_save_game_loaded(_ctx) {
    // _ctx is { save_path }.
    //   .save_path - the path of the save file being loaded.
    // The load is starting, not finished: initialize per-save state
    // here, but do not touch the world yet.
}

// inside your latched register function (see Mod Anatomy):
mmapi_on("save.game_loaded", fresh_start_save_game_loaded);
```

## Engine Wiring

- Seam [`save_game_loaded`](../seams/save_game_loaded.md) dispatches from `gml/scripts/GameplaySystems/Cycle/LoadGame.gml`, an emit right after the engine traces the load and records `Game.last_serde_path = loader.save_path;`.

## See Also

- [save.game_saving](save.game_saving.md) - This hook is the save-side counterpart, and it fires when a save is about to be written.
- [game.save_guard](game.save_guard.md) - Veto saves entirely.
- [game.title_entered](game.title_entered.md) - This hook is the matching end-of-session signal, back on the title screen.
