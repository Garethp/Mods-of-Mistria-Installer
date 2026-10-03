# Seam: player_perk_purchased

Emits at the end of the shrine's `purchase_entry()`, after the essence is spent and the perk is acquired.

`player_perk_purchased` is a **template seam** (`op = "emit"`). It feeds [player.perk_purchased](../hooks/player.perk_purchased.md). Mod authors never write seams. You register handlers for the hooks they dispatch. See [Seams](../SEAMS.md).

## Placement

| | |
| --- | --- |
| **File** | `gml/scripts/UI/Anchor/Menus/DragonshrineMenu.gml` |
| **Locator** | structural target: `purchase_entry`, after `ARI.acquire_perk(entry.perk);` |
| **Op** | `emit` |
| **Feeds** | [`player.perk_purchased`](../hooks/player.perk_purchased.md) |
| **ctx built** | `{ perk: entry.perk }` |
| **Marker** | `mmapi_player_perk_purchased` |

## The Edit

The generated emit lands after the last statement of the `DragonShrineMenu` constructor's `purchase_entry(entry)`. It calls `mmapi_emit("player.perk_purchased", { perk: entry.perk })` in the uniform try/catch shape. The two statements above it are the whole purchase. `ARI.modify_essence(-entry.essence)` spends the price, which is where `player.essence_delta` filters it, and `ARI.acquire_perk(entry.perk)` grants the perk, which is where `player.perk_acquired` fires. The emit runs after both, so a handler sees the purchase complete.

`purchase_entry()` is the Learn button's callback and the only purchase path. The Caldarus statue, the Seridia shrine, and the horse statue all spawn the same menu with a different variant, so one seam covers all three. The debug CLI and the `ALL_UNLOCKS` loop call `acquire_perk()` directly and never pass through here. With zero handlers the seam is behaviorally identical to pristine.

## See Also

- [player.perk_purchased](../hooks/player.perk_purchased.md) - This is the hook this seam dispatches.
- [player_perk_acquired](player_perk_acquired.md) - This is the emit inside `acquire_perk()`, which runs before this one for the same tap.
- [ui_shrine_entry_is_acquired](ui_shrine_entry_is_acquired.md) - This is the wrap in the same file that decides whether the Learn button appears.
