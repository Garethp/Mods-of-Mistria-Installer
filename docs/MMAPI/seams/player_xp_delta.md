# Seam: player_xp_delta

Filters the XP delta at the head of `gain_xp()`, floors the total at zero, and narrows the level celebration to genuine gains.

`player_xp_delta` is a **text seam** (`anchor` + `replace`). It feeds [player.xp_delta](../hooks/player.xp_delta.md). Mod authors never write seams. You register handlers for the hooks they dispatch. See [Seams](../SEAMS.md).

## Placement

| | |
| --- | --- |
| **File** | `gml/scripts/GameplaySystems/Player/Ari.gml` |
| **Locator** | text anchor spanning `gain_xp(skill, xp, silent)` from its head through the level-celebration condition |
| **Op** | text (`anchor` + `replace`) |
| **Feeds** | [`player.xp_delta`](../hooks/player.xp_delta.md) |
| **Value filtered** | `xp` - the XP delta |
| **ctx built** | `{ player: self, skill: skill, silent: silent }` |
| **Marker** | `mmapi_player_run_xp_delta_filters` |

## The Edit

One anchor, two changes, both unreachable in vanilla:

1. **The filter, with its floor.** At the head of `gain_xp()`, the replacement runs `mmapi_apply_filters("player.xp_delta", xp, { player: self, skill: skill, silent: silent })` in a try/catch, adopts the result only when it is numeric (`is_real` or `is_int64`), and floors the adopted value at `-self.skill_xp[skill]`. The engine's capped add - `min(skill_xp[skill] + xp, MAX_SKILL_LEVEL_COSTS[skill])` - then runs on the filtered delta. The floor matters because the engine has no lower clamp of its own and `skill_xp_to_level()` reads a negative total as level `-1`; floored at zero total, the computed level bottoms out at 1, since levels 0 and 1 cost 0 XP.
2. **The celebration narrows.** Pristine celebrates any level change: `if !silent && old_skill_level != ARI.level(skill)` plays the LevelUp sound and spawns the skill dingaling. The replacement changes `!=` to `<`, so a level *down* - reachable only through this hook's negative deltas - passes silently instead of playing a level-up fanfare.

With zero handlers both changes are behaviorally equivalent to pristine: every vanilla delta is non-negative, so the floor never binds and the `<` condition agrees with `!=` everywhere vanilla can reach. `gain_xp` is the engine's sole gameplay XP writer - combat, farming, fishing, mining, archaeology, ranching, and cooking all call it - so one edit filters every skill's XP. Save load and the debug menu write `skill_xp` directly and never pass through.

## See Also

- [player.xp_delta](../hooks/player.xp_delta.md) - This is the hook this seam dispatches.
- [player_essence_delta](player_essence_delta.md) - This seam's sibling: the other delta filter that carries its own floor.
- [statue_hp_death_sweep](statue_hp_death_sweep.md) - The other place the catalog patches an engine gap vanilla cannot reach.
