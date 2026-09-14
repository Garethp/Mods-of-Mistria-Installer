# Hook: player.perk_purchased

Know when the player buys a perk at a shrine.

`player.perk_purchased` is an **event** hook. Register a callback with `mmapi_on`. See [Hooks](../HOOKS.md) for how registration and dispatch work.

## Contract

Fires at the end of `DragonShrineMenu.purchase_entry(entry)`, after the shrine has spent the entry's essence through `Ari.modify_essence()` (so `player.essence_delta` has already run) and after `Ari.acquire_perk()` has run for it (so `player.perk_acquired` has already fired). ctx is `{ perk }`, the `Perk` enum id of the entry the player bought.

This hook is observation only. `purchase_entry()` is the one purchase path in the engine, the Learn button of the Caldarus, Seridia, and horse statue shrine menus, so it fires exactly once per confirmed purchase and never for the debug CLI grant, the `ALL_UNLOCKS` loop, or save load.

| | |
| --- | --- |
| **Fires** | At the end of `DragonShrineMenu.purchase_entry(entry)`, after the essence is spent and the perk is acquired. |
| **ctx** | `{ perk }` |
| **Kind contract** | The callback observes the moment. Its return value is ignored. |

### The ctx struct

- `perk` - the `Perk` enum id of the entry the player bought. `perk_to_string(perk)` names it.

## Usage

```gml
// player.perk_purchased is an EVENT: the return value is ignored.
// You cannot change or stop it here; the return value is ignored.
function perk_ledger_player_perk_purchased(_ctx) {
    // _ctx is { perk }.
    //   .perk - the Perk enum id the player just bought.
    // The essence is spent and ARI.perks[_ctx.perk] is true by now.
    // player.perk_acquired already fired for this same tap, so anything
    // a handler there wrote to ARI.perks_active is visible here.
    // if (_ctx.perk == Perk.GuardiansShield) { ... }
}

// inside your latched register function (see Mod Anatomy):
mmapi_on("player.perk_purchased", perk_ledger_player_perk_purchased);
```

## Interactions

- For one Learn tap the order is `player.essence_delta`, then `player.perk_acquired`, then this hook. A handler here sees the final state the earlier handlers left.
- The Learn button only appears for an entry that `ui.shrine_entry_is_acquired` reports as not acquired. A filter there that forces `false` on an owned perk makes the purchase a re-grant, and this hook fires for it like any other purchase.
- The Caldarus, Seridia, and horse statue menus share this path, and ctx does not say which one the purchase came from.
- The debug CLI grant and the `ALL_UNLOCKS` loop reach `acquire_perk()` directly, so they fire `player.perk_acquired` without ever firing this hook.

## Engine Wiring

- Seam [`player_perk_purchased`](../seams/player_perk_purchased.md) dispatches from `gml/scripts/UI/Anchor/Menus/DragonshrineMenu.gml`, after `ARI.acquire_perk(entry.perk)` in `purchase_entry()`.

## See Also

- [player.perk_acquired](player.perk_acquired.md) - Know when the player has acquired a perk, on every path including this one.
- [player.essence_delta](player.essence_delta.md) - Change every essence gain or spend before it applies, the purchase's price included.
- [ui.shrine_entry_is_acquired](ui.shrine_entry_is_acquired.md) - Change whether a shrine entry counts as already bought.
- [store.item_added](store.item_added.md) - Know when an item lands in the shopping basket.
