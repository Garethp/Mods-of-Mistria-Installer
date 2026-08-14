# Engine Fix: tarball_chop_burn_flag

Passes the tarball's real fire flag to its grid chop, so non-fire chops stop being burn-throttled by stump/fruit-tree iframes.

`tarball_chop_burn_flag` is an **engine fix**, an anchored edit with no hook behind it. Nothing dispatches. The corrected argument is the whole feature. See [Seams](../SEAMS.md).

## Placement

| | |
| --- | --- |
| **File** | `gml/objects/Combat/obj_damage_tarball.gml` |
| **Locator** | text anchor: the `chop_node(...)` call in the `can_chop_grid_objects` block, plus the fire-sound check that follows it |
| **Feeds** | (no hook) |
| **Marker** | `mmapi_tarball_chop_burn_flag` |

## The Edit

The anchor is the grid-chop call inside `obj_damage_tarball`'s `can_chop_grid_objects` block:

```gml
var success = chop_node(GRID, xx, yy, axe_prototype, 5, doppel, true);
if success && has_flag(self.flags, CombatFlag.Fire) {
```

The replace changes one argument. The chop's `is_burn` becomes `has_flag(self.flags, CombatFlag.Fire)` instead of the hardcoded `true`. That is exactly the expression the pick block above already passes to `pick_node`, and the very next line already tests it for the fire sound. The corrected line carries the `mmapi_tarball_chop_burn_flag` marker as a trailing comment.

Burn mode is `chop_node`'s throttle for fire damage over time. While a node's `burn_iframes` counter is positive, each burn-flagged chop call only decrements it and returns, dealing no damage. Stumps arm at 100, fruit trees at 600, and regular trees at 0. A tree felled by a burn-flagged chop also writes its leftover stump pre-armed at 100. With the hardcoded `true`, every tarball-driven chop was throttled this way, fire or not. A mod that sets `can_chop_grid_objects` on sword swings or the jump slam (e.g. a utility-sword mod) had its slams silently absorbed by stump iframes. The pound landed, the chop calls all happened, and nothing took damage until 2-3 slams had drained the counter. Fruit trees showed the same wall at 600. With the flag passed through, non-fire tarball chops apply normal chop damage on the first call, while genuine fire sources (fire breath, fire bombs) keep their intended burn throttle unchanged.

## See Also

- [game_step_begin_installs](game_step_begin_installs.md) - This is another of the catalog's engine fixes, the MMAPI lifecycle root.
- [node_renderer_set_sprite](node_renderer_set_sprite.md) - The node renderer's sprite filter, elsewhere in the same combat-adjacent draw path.
