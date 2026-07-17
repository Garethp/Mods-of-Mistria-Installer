# Seam: save_game_saving

Announces an imminent save, right after the engine records the save path.

`save_game_saving` is a **template seam** (`op = "emit"`). It feeds [save.game_saving](../hooks/save.game_saving.md). Mod authors never write seams. You register handlers for the hooks they dispatch. See [Seams](../SEAMS.md).

## Placement

| | |
| --- | --- |
| **File** | `gml/scripts/Serialization/SaveGame.gml` |
| **Locator** | structural target: `save_game`, after `Game.last_serde_path = save_path;` |
| **Op** | `emit` |
| **Feeds** | [`save.game_saving`](../hooks/save.game_saving.md) |
| **ctx built** | `{ save_path: save_path }` |
| **Marker** | `mmapi_modsave_run_save` |

## The Edit

The generated emit is placed structurally inside `save_game`, immediately after `Game.last_serde_path = save_path;`, the line that records where this save is going. That puts it after [game_save_guard](game_save_guard.md)'s head-of-function check has allowed and after the saver is created, but before the save is written. It calls `mmapi_emit("save.game_saving", { save_path: save_path })` in the uniform try/catch shape (`catch_var = "__mmapi_modsave_save"`).

Handlers get the definitive `save_path`, the moment to persist mod state that should travel with this save, before the game serializes. With zero handlers the seam is behaviorally identical to pristine.

## See Also

- [save.game_saving](../hooks/save.game_saving.md) - This is the hook this seam dispatches.
- [game_save_guard](game_save_guard.md) - This is the same file's guard, and a veto there means this emit never runs.
- [save_game_loaded](save_game_loaded.md) - This is the load-side counterpart.
