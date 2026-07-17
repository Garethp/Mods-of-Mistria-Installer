# Hook: save.game_saving

Know the moment the game commits to writing a save.

`save.game_saving` is an **event** hook. Register a callback with `mmapi_on`. See [Hooks](../HOOKS.md) for how registration and dispatch work.

## Contract

Fires at the top of `save_game()`, after [game.save_guard](game.save_guard.md) allows and the saver is created, before the save is written. ctx is `{ save_path }`.

| | |
| --- | --- |
| **Fires** | At the top of `save_game()`, after `game.save_guard` allows and the saver is created, before the save is written. |
| **ctx** | `{ save_path }` |
| **Kind contract** | The callback observes the moment. Its return value is ignored. |

### The ctx struct

- `save_path` - the path the save is about to be written to, the same `save_path` the engine records as `Game.last_serde_path`.

> [!NOTE]
> By the time this fires, the save is going to happen: a `game.save_guard` veto would have stopped `save_game()` before this point. Use this event to flush your mod's own state so it lands in (or alongside) the save being written.

## Usage

```gml
// save.game_saving is an EVENT: mmapi calls you after it happens.
// You cannot change or stop it here; the return value is ignored.
function backup_buddy_save_game_saving(_ctx) {
    // _ctx is { save_path }.
    //   .save_path - the path the save is about to be written to.
    // The save is definitely happening (game.save_guard already allowed).
    // Flush your mod's own state here so it rides along with the save.
}

// inside your latched register function (see Mod Anatomy):
mmapi_on("save.game_saving", backup_buddy_save_game_saving);
```

## Engine Wiring

- Seam [`save_game_saving`](../seams/save_game_saving.md) dispatches from `gml/scripts/Serialization/SaveGame.gml`, an emit inside `save_game()` right after `Game.last_serde_path = save_path;`. The [`game_save_guard`](../seams/game_save_guard.md) seam's head-of-function veto sits above it, which is why this event fires only for allowed saves.

## See Also

- [game.save_guard](game.save_guard.md) - This is the veto point at the head of the same function. A vetoed save never reaches this event.
- [save.game_loaded](save.game_loaded.md) - This is the counterpart when a save starts loading.
