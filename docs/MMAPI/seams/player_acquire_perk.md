# Seam: player_acquire_perk

Emits at the head of `acquire_perk()`.

`player_acquire_perk` is a **template seam** (`op = "emit"`). It feeds [player.acquire_perk](../hooks/player.acquire_perk.md). Mod authors never write seams. You register handlers for the hooks they dispatch. See [Seams](../SEAMS.md).

## Placement

| | |
| --- | --- |
| **File** | `gml/scripts/GameplaySystems/Player/Ari.gml` |
| **Locator** | structural target: `acquire_perk`, at head |
| **Op** | `emit` |
| **Feeds** | [`player.acquire_perk`](../hooks/player.acquire_perk.md) |
| **ctx built** | `{ perk: perk }` |
| **Marker** | `mmapi_player_acquire_perk` |

## The Edit

The generated emit lands at the head of the `Ari` struct's `acquire_perk(perk)`. It calls `mmapi_emit("player.acquire_perk", { perk: perk })` in the uniform try/catch shape. The head placement is what lets a handler read `ARI.perks[perk]` pre-write. The owned and active flags, the per-perk side effects (Guardian's Shield, Ancient Inspiration), the stats entry, and the achievements refresh all run after the emit.

Every engine acquisition routes through this one method: the Dragonshrine purchase menu, the debug CLI, and the `ALL_UNLOCKS` grant-all loop. Save load writes the perk arrays wholesale and never calls it, and the enable/disable toggles (the shrine menu's and the debug CLI's) flip `perks_active` directly, so toggles never emit either. With zero handlers the seam is behaviorally identical to pristine.

## See Also

- [player.acquire_perk](../hooks/player.acquire_perk.md) - This is the hook this seam dispatches.
- [player_heal_vfx](player_heal_vfx.md) - This is the neighboring guard in the same file.
- [player_essence_delta](player_essence_delta.md) - This is the filter the Dragonshrine purchase's essence cost passes through, right before this emit.
