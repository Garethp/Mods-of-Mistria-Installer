# Seam: player_heal_vfx

Puts a veto check at the head of `play_heal_vfx()`.

`player_heal_vfx` is a **template seam** (`op = "guard"`). It feeds [player.heal_vfx](../hooks/player.heal_vfx.md). Mod authors never write seams. You register handlers for the hooks they dispatch. See [Seams](../SEAMS.md).

## Placement

| | |
| --- | --- |
| **File** | `gml/scripts/GameplaySystems/Player/Ari.gml` |
| **Locator** | structural target: `play_heal_vfx`, at head |
| **Op** | `guard` |
| **Feeds** | [`player.heal_vfx`](../hooks/player.heal_vfx.md) |
| **ctx built** | `{ color: color, sprite: sprite }` |
| **On veto** | `return;` |
| **Marker** | `mmapi_player_run_heal_vfx_guards` |

## The Edit

The generated dispatch lands at the head of `play_heal_vfx()`, before any of the effect's setup. It calls `mmapi_check_guards("player.heal_vfx", { color: color, sprite: sprite })` in the uniform try/catch shape. When any guard returns `false`, the injected line runs `return;` and the heal effect never plays. `undefined` or `true` lets the effect proceed. The ctx hands guards the function's own arguments, the effect's color and sprite, which is what a handler keys off to veto some heals and not others.

The locator is structural (function + head, matched token-wise), immune to whitespace and comment drift in `Ari.gml`. The guard only suppresses the visual: the health change that prompted it has already gone through `modify_health` and is untouched. With zero handlers the seam is behaviorally identical to pristine.

## See Also

- [player.heal_vfx](../hooks/player.heal_vfx.md) - This is the hook this seam dispatches.
- [player_health_delta](player_health_delta.md) - This is the filter on the health change itself, in the same file.
