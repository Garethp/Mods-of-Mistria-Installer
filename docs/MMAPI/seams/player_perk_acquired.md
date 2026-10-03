# Seam: player_perk_acquired

Emits at the end of `acquire_perk()`, after the perk flags, side effects, stats entry, and achievements refresh.

`player_perk_acquired` is a **template seam** (`op = "emit"`). It feeds [player.perk_acquired](../hooks/player.perk_acquired.md). Mod authors never write seams. You register handlers for the hooks they dispatch. See [Seams](../SEAMS.md).

## Placement

| | |
| --- | --- |
| **File** | `gml/scripts/GameplaySystems/Player/Ari.gml` |
| **Locator** | structural target: `acquire_perk`, after `refresh_achievements([Requirement.HasAtLeastOneTierFivePerkPerCategory]);` |
| **Op** | `emit` |
| **Feeds** | [`player.perk_acquired`](../hooks/player.perk_acquired.md) |
| **ctx built** | `{ perk: perk }` |
| **Marker** | `mmapi_player_perk_acquired` |

## The Edit

The generated emit lands after the last statement of the `Ari` struct's `acquire_perk(perk)`. It calls `mmapi_emit("player.perk_acquired", { perk: perk })` in the uniform try/catch shape. Everything the engine does for an acquisition has run by then. The owned and active flags are written, the side effects that belong to particular perks (Guardian's Shield, Ancient Inspiration) have fired, the `GAME_STATS.perk_acquirements` entry is pushed, and the `HasAtLeastOneTierFivePerkPerCategory` achievement requirement has been refreshed. That is what lets a handler write `ARI.perks_active[perk]` and have the value stand, since nothing in the function runs after the emit.

Every engine acquisition routes through this one method, whether from the Dragonshrine purchase menu, the debug CLI, or the `ALL_UNLOCKS` loop. Save load writes the perk arrays wholesale and never calls it, and the enable and disable toggles (the shrine menu's and the debug CLI's) flip `perks_active` directly, so toggles never emit either. With zero handlers the seam is behaviorally identical to pristine.

## See Also

- [player.perk_acquired](../hooks/player.perk_acquired.md) - This is the hook this seam dispatches.
- [player_perk_purchased](player_perk_purchased.md) - This is the emit at the end of the shrine's purchase, which runs after this one for the same tap.
- [player_heal_vfx](player_heal_vfx.md) - This is the neighboring guard in the same file.
- [player_essence_delta](player_essence_delta.md) - This is the filter the Dragonshrine purchase's essence cost passes through, before this emit.
