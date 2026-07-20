# Seam: game_save_guard

Puts a veto check at the head of `save_game()`, before anything is written.

`game_save_guard` is a **template seam** (`op = "guard"`). It feeds [game.save_guard](../hooks/game.save_guard.md). Mod authors never write seams. You register handlers for the hooks they dispatch. See [Seams](../SEAMS.md).

## Placement

| | |
| --- | --- |
| **File** | `gml/scripts/Serialization/SaveGame.gml` |
| **Locator** | structural target: `save_game`, at head |
| **Op** | `guard` |
| **Feeds** | [`game.save_guard`](../hooks/game.save_guard.md) |
| **ctx built** | `{ save_path: save_path }` |
| **On veto** | `return;` |
| **Marker** | `mmapi_game_run_save_guards` |
| **Depends on** | [`save_game_saving`](save_game_saving.md) |

## The Edit

The generated guard lands at the head of `save_game()`, before the saver is created and before anything touches disk. It calls `mmapi_check_guards("game.save_guard", { save_path: save_path })`. When any guard returns `false`, the injected line runs `return;`. No saver is created, no file is written, and [save.game_saving](../hooks/save.game_saving.md) never fires (its emit sits later in the same function). With zero handlers the seam is behaviorally identical to pristine.

`game_save_guard` is one of the catalog's late entries, appended after [save_game_saving](save_game_saving.md) has already seamed `SaveGame.gml`. Catalog order is apply order, and stacking onto an already-seamed file works because this seam's locator is pristine text that the earlier seam's edit re-emits verbatim, so it matches exactly once against both the pristine file and the staged file. `depends_on = ["save_game_saving"]` makes the ordering explicit.

## See Also

- [game.save_guard](../hooks/game.save_guard.md) - This is the hook this seam dispatches.
- [save_game_saving](save_game_saving.md) - This is the same file's emit, which fires only after this guard allows.
- [save_game_loaded](save_game_loaded.md) - This is the load-side counterpart.
