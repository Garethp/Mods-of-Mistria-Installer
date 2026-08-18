# Seam: renown_gains

Emits renown level and rank gains inside `set_renown()`, past its gains-only early return.

`renown_gains` is a **text seam** (`anchor` + `replace`). It feeds [renown.level_gained](../hooks/renown.level_gained.md) and [renown.rank_gained](../hooks/renown.rank_gained.md). Mod authors never write seams. You register handlers for the hooks they dispatch. See [Seams](../SEAMS.md).

## Placement

| | |
| --- | --- |
| **File** | `gml/scripts/GameplaySystems/Player/Ari.gml` |
| **Locator** | text anchor inside `set_renown()`: the gains-only early return plus the rewards-count line that follows it |
| **Feeds** | [`renown.level_gained`](../hooks/renown.level_gained.md), [`renown.rank_gained`](../hooks/renown.rank_gained.md) |
| **ctx built** | `{ old_level: level, new_level: new_level }` and `{ old_rank, new_rank }` |
| **Marker** | `mmapi_renown_run_gains` |

## The Edit

The injected emits land between `set_renown()`'s early return and the reward loop. The pristine function computes the level before and after the write and bails with `if sign(new_levels) != 1`, so only writes that raise the level reach this point. That is the placement doing the semantic work. The hooks' names promise gains, and the engine's own gate delivers them.

The first line emits `renown.level_gained` with the before and after levels. The second block computes the before and after rank indexes (`level div RENOWN.levels_per_rank`, clamped to the last rank the way `renown_level_to_rank()` clamps) and emits `renown.rank_gained` only when they differ. This is a text seam because that boundary comparison is a conditional the template ops cannot express. Both emits run before the crossed levels' rewards are granted and before the renown level quests start.

`set_renown` is where every renown write lands. The day-rollover drain arrives through `modify_renown` (already filtered by [player.renown_delta](../hooks/player.renown_delta.md)), and debug sets call it directly. Save load restores the field without calling it, so loading never replays gains. With zero handlers the seam is behaviorally identical to pristine.

## See Also

- [renown.level_gained](../hooks/renown.level_gained.md) - This is the first hook this seam dispatches.
- [renown.rank_gained](../hooks/renown.rank_gained.md) - This is the second, behind the rank-boundary comparison.
- [player_renown_delta](player_renown_delta.md) - This is the filter upstream in `modify_renown()`, before any write reaches `set_renown()`.
