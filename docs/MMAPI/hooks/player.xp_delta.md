# Hook: player.xp_delta

Change every skill XP gain before it applies, or turn it into a deduction.

`player.xp_delta` is a **filter** hook. Register a callback with `mmapi_filter`. See [Hooks](../HOOKS.md) for how registration and dispatch work.

## Contract

Fires at the top of `Ari.gain_xp(skill, xp, silent)`, before the XP is applied. The filtered value is the XP delta. ctx is `{ player, skill, silent }`; `skill` is the `Skill.*` enum id, so one handler covers every skill and can branch on the one it cares about. Return the replacement delta, or `undefined` to keep the current value.

A negative delta genuinely deducts XP and can lower the skill's level. The seam makes that safe: it floors the filtered result at `-skill_xp[skill]`, so the running total never goes negative.

The engine's own cap at `MAX_SKILL_LEVEL_COSTS` still applies after the filter, and already-granted level rewards are not revoked by leveling down. A mod that wants to deduct XP outright (rather than transform an engine gain) calls `ARI.gain_xp(skill, -amount)` directly. The deduction routes through this filter and the same floor.

| | |
| --- | --- |
| **Fires** | At the top of `Ari.gain_xp(skill, xp, silent)`, before the XP is applied. |
| **Value** | The XP delta. Vanilla deltas are always gains; a negative replacement deducts. |
| **ctx** | `{ player, skill, silent }` |
| **Kind contract** | The callback receives the current value and returns a replacement, or `undefined` to keep the current value. The seam floors the result at `-skill_xp[skill]`. |

### The ctx struct

- `player` - the `Ari` struct gaining the XP.
- `skill` - the `Skill.*` enum id of the skill the XP applies to.
- `silent` - the `silent` flag `gain_xp` received: whether the engine caller asked to skip the level-up celebration.

## Usage

```gml
// player.xp_delta is a FILTER: you receive (value, ctx) and return a
// replacement, or undefined to keep the game's value.
function battle_scholar_player_xp_delta(_value, _ctx) {
    // _value is the XP delta. Negative deducts; the seam floors your return
    // at -skill_xp[skill], so the total never goes below zero.
    // _ctx is { player, skill, silent }.
    //   .player - the Ari struct gaining the XP.
    //   .skill  - the Skill.* enum id the XP applies to.
    //   .silent - whether the caller asked to skip the celebration.
    if (_value == undefined) return undefined; // test undefined BEFORE anything else
    if (_ctx.skill == Skill.Combat) return _value * 2; // double Combat XP only
    return undefined; // undefined = keep every other skill unchanged
}

mmapi_filter("player.xp_delta", battle_scholar_player_xp_delta);
```

## Engine Wiring

- Seam [`player_xp_delta`](../seams/player_xp_delta.md) dispatches from `gml/scripts/GameplaySystems/Player/Ari.gml`, at the head of `gain_xp(skill, xp, silent)`, filtering `xp` and flooring the result before the engine's capped add. The same edit narrows the level celebration from any level change to a genuine gain.

## See Also

- [player.essence_delta](player.essence_delta.md) - The similar filter shape for essence change.
- [npc.heart_points](npc.heart_points.md) - The equivalent delta filter for villager hearts.
- [animal.heart_points](animal.heart_points.md) - The equivalent delta filter for barn animals.
