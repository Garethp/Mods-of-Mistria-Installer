# Hook: game.save_guard

Block a game save before anything is written.

`game.save_guard` is a **guard** hook. Register a callback with `mmapi_guard`. See [Hooks](../HOOKS.md) for how registration and dispatch work.

## Contract

Fires at the top of `save_game()`, before the saver is created. ctx is `{ save_path }`. Return `false` to veto the save. `undefined` or `true` allows.

| | |
| --- | --- |
| **Fires** | At the top of `save_game()`, before the saver is created. |
| **ctx** | `{ save_path }` |
| **Kind contract** | The callback returns `false` to veto the action. `undefined` or `true` allows it. Guards fail open: a callback that throws counts as allow. |

### The ctx struct

- `save_path` - the path `save_game()` was asked to write to.

> [!NOTE]
> A vetoed save stops at the head of `save_game()`: no saver is created, nothing is written, and [save.game_saving](save.game_saving.md) never fires. That event only fires after this guard allows.

## Usage

```gml
// game.save_guard is a GUARD: return false to block it, undefined (or true)
// to allow. Guards fail OPEN - if your handler crashes, the action happens.
function ironman_rules_game_save_guard(_ctx) {
    // _ctx is { save_path }.
    //   .save_path - the path save_game() was asked to write to.
    if (!__ironman_rules_runtime().enabled) return undefined;
    // if (<saving is forbidden right now>) {
    //     return false; // veto - the engine then runs: return;
    // }
    return undefined; // allow everything else
}

// inside your latched register function (see Mod Anatomy):
mmapi_guard("game.save_guard", ironman_rules_game_save_guard);
```

## Engine Wiring

- Seam [`game_save_guard`](../seams/game_save_guard.md) dispatches from `gml/scripts/Serialization/SaveGame.gml`, at the head of `save_game()`. On veto the engine runs `return;`. It depends on [`save_game_saving`](../seams/save_game_saving.md), whose emit sits later in the same function, so an allowed save flows straight into `save.game_saving`.

## See Also

- [save.game_saving](save.game_saving.md) - This event fires only after this guard allows, once the saver is created.
- [save.game_loaded](save.game_loaded.md) - This event is the load-side counterpart.
