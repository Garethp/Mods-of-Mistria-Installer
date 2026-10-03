# Seam: player_jump_attack_landing

Filters whether the jump attack may land, once for each corner of the player's bounding box.

`player_jump_attack_landing` is a **text seam** (`anchor` + `replace`). It feeds [player.jump_attack_landing](../hooks/player.jump_attack_landing.md). Mod authors never write seams. You register handlers for the hooks they dispatch. See [Seams](../SEAMS.md).

## Placement

| | |
| --- | --- |
| **File** | `gml/scripts/Player/AriFsm.gml` |
| **Locator** | text anchor on the `owner.check_bbox_at_point(...)` call that `can_down_smash_on_pos()` returns inside the `PlayerState.Jump` create, through the corner predicate's closing line |
| **Op** | text (`anchor` + `replace`) |
| **Feeds** | [`player.jump_attack_landing`](../hooks/player.jump_attack_landing.md) |
| **Value filtered** | the boolean the corner predicate returns, `true` for a node that is neither collideable nor water |
| **ctx built** | `{ x: xx, y: yy, node: ni, collideable: GRID.node_collideable[ni], water: GRID.node_terrain_kind[ni] == TerrainKind.Water }` |
| **Marker** | `mmapi_player_run_jump_attack_landing_filters` |

## The Edit

`can_down_smash_on_pos()` is a function the Jump state declares in its create and calls from its step, once per frame after the jump's peak once the player has pressed the attack input during the jump. It first refuses a landing over a monster or NPC, then returns `owner.check_bbox_at_point()` over the player's position with a corner predicate. The predicate looks up the corner's grid node, returns `false` for a corner outside the grid, and otherwise returns whether the node is neither collideable nor water. `check_bbox_at_point()` combines the four corners with `&&`, so the attack lands only when every corner passes.

The replacement keeps the node lookup and the return for a corner outside the grid. It evaluates the pristine expression once into `__mmapi_landing_ok`, filters that boolean through `mmapi_apply_filters("player.jump_attack_landing", ...)` with the corner's position, node index, collision flag, and a water boolean, and returns the final value. The monster and NPC refusal runs before the corner pass and never dispatches. A catch at the site leaves the pristine value in place when context construction or dispatch fails.

The ctx carries the water test as a boolean rather than the terrain kind value. `TerrainKind` is a native enum with no declaration in the GML tree.

With zero handlers the filter returns the pristine expression unchanged, so the landing test refuses the same corners as pristine. The dispatch runs for at most four corners on each frame the test runs, and `check_bbox_at_point()` stops at the first failing corner, so a refused landing dispatches about once per frame from the jump's peak until the jump ends.

## See Also

- [player.jump_attack_landing](../hooks/player.jump_attack_landing.md) - This is the hook this seam dispatches.
- [player_jump_attack_water_exit](player_jump_attack_water_exit.md) - This is the companion edit that sends a water landing's exit to the Swim state.
- [fsm_transition](fsm_transition.md) - Filters the state transitions that follow the landing test.
