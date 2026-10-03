# Seam: player_jump_attack_water_exit

Ends a jump attack that landed on water in the Swim state instead of Default.

`player_jump_attack_water_exit` is a **text seam** (`anchor` + `replace`). It supports [player.jump_attack_landing](../hooks/player.jump_attack_landing.md) without dispatching a hook. Mod authors never write seams. You register handlers for the hooks they dispatch. See [Seams](../SEAMS.md).

## Placement

| | |
| --- | --- |
| **File** | `gml/scripts/Player/AriFsm.gml` |
| **Locator** | text anchor on the `AnimationName.DownAttack` animation line in the `PlayerState.DownSmash` start, through the `on_animation_complete` assignment that follows it |
| **Op** | text (`anchor` + `replace`) |
| **Feeds** | [`player.jump_attack_landing`](../hooks/player.jump_attack_landing.md), as a companion edit with no dispatch |
| **Marker** | `mmapi_player_jump_attack_water_exit` |

## The Edit

The DownSmash state's start sets `on_animation_complete` to `PlayerState.Default`, and the player object changes to that state when the attack animation completes. Pristine code can rely on Default because the landing test refuses water. A handler on the hook can allow a water landing, and the Default state would then stand the player on the water. Player collision tests only a node's collision flag, so the player would walk on the surface until the next jump.

The replacement keeps the Default assignment and adds a test after it. It reads the grid node under the player's position with `GRID.try_node_index_for_room_position()`, and when that node's terrain is water it sets `on_animation_complete` to `PlayerState.Swim` instead. This is the test the Jump state's own landing branch makes when a plain jump ends over water, so a landing with that node on land stands on the shore and a landing on open water swims. The player's position never changes during the DownSmash state, so the terrain at start is the terrain at exit.

The edit adds no splash, sound, or rumble. Those belong to the Jump state's landing branch, which the attack never reaches.

Vanilla never enters the DownSmash state over water, so with zero handlers the water branch is never taken and the exit is Default as in pristine.

## See Also

- [player.jump_attack_landing](../hooks/player.jump_attack_landing.md) - This is the hook this edit supports.
- [player_jump_attack_landing](player_jump_attack_landing.md) - This is the dispatch site whose final value can allow the water landing.
- [fsm_transition](fsm_transition.md) - Filters the exit transition this edit chooses.
