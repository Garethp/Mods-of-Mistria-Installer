# Hook: player.perk_acquired

Know when the player has acquired a perk.

`player.perk_acquired` is an **event** hook. Register a callback with `mmapi_on`. See [Hooks](../HOOKS.md) for how registration and dispatch work.

## Contract

Fires at the end of `Ari.acquire_perk(perk)`, after the perk is flagged owned and active, after its acquisition side effects (the Guardian's Shield extra invulnerable hit, the Ancient Inspiration timer reset) have run, and after the stats entry is pushed and the achievements refresh. ctx is `{ perk }`, the `Perk` enum id.

This hook is observation only, and the flags are already written when handlers run, so a handler that sets `ARI.perks_active[perk]` itself has the last word on whether the perk takes effect. It fires on every acquisition path (the Dragonshrine purchase, the debug CLI grant, and the `ALL_UNLOCKS` loop that grants every perk to a new game) but never on save load, which restores the perk arrays directly. Toggling an owned perk on or off never fires either, because the shrine menu and the debug CLI flip `perks_active` directly without entering `acquire_perk()`. In vanilla play every acquisition is a first acquisition, since the shrine only offers Learn for an unowned perk.

| | |
| --- | --- |
| **Fires** | At the end of `Ari.acquire_perk(perk)`, after the perk flags, the side effects, the stats entry, and the achievements refresh. |
| **ctx** | `{ perk }` |
| **Kind contract** | The callback observes the moment. Its return value is ignored. |

### The ctx struct

- `perk` - the `Perk` enum id just acquired. `perk_to_string(perk)` names it.

## Usage

```gml
// player.perk_acquired is an EVENT: the return value is ignored.
// You cannot change or stop it here; the return value is ignored.
function perk_fanfare_player_perk_acquired(_ctx) {
    // _ctx is { perk }.
    //   .perk - the Perk enum id just acquired.
    // ARI.perks[_ctx.perk] and ARI.perks_active[_ctx.perk] are both true
    // already, and nothing in acquire_perk runs after this emit, so a
    // write here stands:
    // if (<your condition>) ARI.perks_active[_ctx.perk] = false; // owned, switched off
}

// inside your latched register function (see Mod Anatomy):
mmapi_on("player.perk_acquired", perk_fanfare_player_perk_acquired);
```

## Interactions

- A handler that writes `ARI.perks_active[ctx.perk] = false` leaves the perk owned but switched off, the same state the shrine popup's Disable button produces. `ARI.perk_active()` reads false until something switches it back on.
- The shrine popup's Enable button writes `ARI.perks_active` directly and fires nothing, so the player can switch back on a perk that a handler switched off.
- The Guardian's Shield extra invulnerable hit and the Ancient Inspiration timer reset have already run. Switching the perk off does not take the extra hit back.
- The `HasAtLeastOneTierFivePerkPerCategory` achievement refresh has already run, and it reads only the owned flag, so a Steam unlock it granted is out of a handler's reach.
- On the shrine path, `player.essence_delta` has already filtered the cost before this hook, and `player.perk_purchased` fires after it for the same tap.

## Engine Wiring

- Seam [`player_perk_acquired`](../seams/player_perk_acquired.md) dispatches from `gml/scripts/GameplaySystems/Player/Ari.gml`, after the last statement of `acquire_perk()`.

## See Also

- [player.perk_purchased](player.perk_purchased.md) - Know when the player buys a perk at a shrine, the one acquisition path with a price.
- [player.essence_delta](player.essence_delta.md) - The Dragonshrine purchase's essence cost routes through this filter, right before the perk is acquired.
- [player.max_health_item](player.max_health_item.md) - Know when an item permanently raises Ari's max health.
- [player.skill_leveled](player.skill_leveled.md) - Know the moment the player levels up a skill.
