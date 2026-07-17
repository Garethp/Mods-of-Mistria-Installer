# Seam: combat_tarball_grid

Hands every active swing's tarball to mods before the grid pick/chop/destroy blocks read it.

`combat_tarball_grid` is a **template seam** (`op = "filter_call"`, in-place: the dispatch's return value is discarded). It feeds [combat.tarball_grid](../hooks/combat.tarball_grid.md). Mod authors never write seams. You register handlers for the hooks they dispatch. See [Seams](../SEAMS.md).

## Placement

| | |
| --- | --- |
| **File** | `gml/objects/Combat/obj_damage_tarball.gml` |
| **Locator** | pristine context: the top of the `if self.can_hurt {` block in the tarball's step, before `var col_count = 0;` |
| **Op** | `filter_call` (in-place) |
| **Feeds** | [`combat.tarball_grid`](../hooks/combat.tarball_grid.md) |
| **Value filtered** | `self` - the tarball instance, mutated in place |
| **ctx built** | `undefined` |

## The Edit

The generated dispatch lands at the top of `obj_damage_tarball`'s `can_hurt` block, before everything that block does with the tarball: the grid pick block (`obj_damage_tarball.gml:168`), the chop block (`:209`), the destroy block (`:231`), and the receiver-collision enqueue (`:253`). Because the op is `filter_call`, the dispatch runs `mmapi_apply_filters("combat.tarball_grid", self, ...)` and throws the result away. Handlers mutate the tarball they are given, never build a replacement.

That ordering is the whole point: the flags a handler sets (`can_pick_grid_objects`, `can_chop_grid_objects`, `can_destroy_grid_objects`) are exactly what the pick/chop/destroy blocks read, in the same frame, immediately after the dispatch. Set them and the swing forages, chops, or destroys grid nodes that step. Handlers should guard on the tarball's target (`CombatTarget.Player` marks an incoming hit on the player) and provenance before acting. This runs for every active `can_hurt` tarball every unpaused frame, a hot path.

## See Also

- [combat.tarball_grid](../hooks/combat.tarball_grid.md) - This is the hook that this seam dispatches.
- [combat_damage_pre](combat_damage_pre.md) - This is the receiver-side filter that the same tarball flows through when it hits something.
- [combat_damage_resolved](combat_damage_resolved.md) - The resolution emits at the end of the tarball's journey.
