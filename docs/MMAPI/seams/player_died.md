# Seam: player_died

Emits on the final death path, right after the dying scene starts.

`player_died` is a **template seam** (`op = "emit"`). It feeds [player.died](../hooks/player.died.md). Mod authors never write seams. You register handlers for the hooks they dispatch. See [Seams](../SEAMS.md).

## Placement

| | |
| --- | --- |
| **File** | `gml/objects/characters/obj_ari.gml` |
| **Locator** | structural target: `should_die`, at after `MIST.run_scene("dying");` |
| **Op** | `emit` |
| **Feeds** | [`player.died`](../hooks/player.died.md) |
| **ctx built** | `self` (the `obj_ari` instance) |
| **Marker** | `mmapi_player_died` |

## The Edit

The generated emit lands inside `should_die()`, in the screen-fade callback's else branch, immediately after `MIST.run_scene("dying");`. It calls `mmapi_emit("player.died", self)` in the uniform try/catch shape. The anchor sits two closures deep (the FSM's `on_start_action`, then the `SCREEN_FADER.fade_out` callback), which is why the emit fires only after the death animation has held and the fade has completed. Both closures are created in the instance's scope, so `self` is the `obj_ari` instance when the emit runs.

The branch placement encodes the hook's meaning. `should_die()` returns early for health above zero and for the Fairy status revive, and its averted-death branch calls `pass_out()` instead of running the scene, so none of those paths reach the emit. It fires exactly when the dying scene starts. With zero handlers the seam is behaviorally identical to pristine.

## See Also

- [player.died](../hooks/player.died.md) - This is the hook this seam dispatches.
- [player_pass_out](player_pass_out.md) - This is the sibling emit on the collapse and averted-death paths.
- [player_incoming_damage](player_incoming_damage.md) - This is the filter on the damage that gets the player here.
