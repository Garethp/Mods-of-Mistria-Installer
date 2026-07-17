# Seam: save_game_loaded

Announces the start of a save load, right after the save path is recorded.

`save_game_loaded` is a **template seam** (`op = "emit"`). It feeds [save.game_loaded](../hooks/save.game_loaded.md). Mod authors never write seams. You register handlers for the hooks they dispatch. See [Seams](../SEAMS.md).

## Placement

| | |
| --- | --- |
| **File** | `gml/scripts/GameplaySystems/Cycle/LoadGame.gml` |
| **Locator** | pristine context after `Game.last_serde_path = loader.save_path;` |
| **Op** | `emit` |
| **Feeds** | [`save.game_loaded`](../hooks/save.game_loaded.md) |
| **ctx built** | `{ save_path: loader.save_path }` |
| **Marker** | `mmapi_modsave_run_load` |

## The Edit

The locator is pristine context: the emit is generated immediately after `Game.last_serde_path = loader.save_path;`, the line that records the incoming save's path at the start of the load (right after the engine traces `"Loading save: {}"`). It calls `mmapi_emit("save.game_loaded", { save_path: loader.save_path })` in the uniform try/catch shape (`catch_var = "__mmapi_modsave_load"`).

Handlers learn which save is coming in as the load begins, the natural moment to read back whatever mod state [save.game_saving](../hooks/save.game_saving.md) handlers persisted under the same path. With zero handlers the seam is behaviorally identical to pristine.

## See Also

- [save.game_loaded](../hooks/save.game_loaded.md) - This is the hook this seam dispatches.
- [save_game_saving](save_game_saving.md) - This seam is the save-side emit with the matching `save_path` ctx.
- [game_save_guard](game_save_guard.md) - This seam is the save-side veto.
