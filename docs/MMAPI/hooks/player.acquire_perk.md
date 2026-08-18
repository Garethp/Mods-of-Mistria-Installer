# Hook: player.acquire_perk

Know when the player acquires a perk.

`player.acquire_perk` is an **event** hook. Register a callback with `mmapi_on`. See [Hooks](../HOOKS.md) for how registration and dispatch work.

## Contract

Fires at the top of `Ari.acquire_perk(perk)`, before the perk is flagged owned and active and before its acquisition side effects (the Guardian's Shield extra invulnerable hit, the Ancient Inspiration timer reset) run. ctx is `{ perk }`, the `Perk` enum id.

This hook is observation only. It fires on every acquisition path: the Dragonshrine purchase (the essence is already spent when the emit runs), the debug CLI grant, and the `ALL_UNLOCKS` new-game grant-all loop. It never fires on save load, which restores the perk arrays directly. Toggling an owned perk on or off never fires either. The shrine menu and the debug CLI flip `perks_active` directly, without entering `acquire_perk()`. At emit time `ARI.perks[ctx.perk]` still reads its old value, so a handler can tell a fresh acquisition from a re-grant.

| | |
| --- | --- |
| **Fires** | At the top of `Ari.acquire_perk(perk)`, before the perk flags are written. |
| **ctx** | `{ perk }` |
| **Kind contract** | The callback observes the moment. Its return value is ignored. |

### The ctx struct

- `perk` - the `Perk` enum id being acquired. `perk_to_string(perk)` names it.

## Usage

```gml
// player.acquire_perk is an EVENT: the return value is ignored.
// You cannot change or stop it here; the return value is ignored.
function perk_fanfare_player_acquire_perk(_ctx) {
    // _ctx is { perk }.
    //   .perk - the Perk enum id being acquired.
    // ARI.perks[_ctx.perk] is not written yet, so a false read here
    // means a genuinely new perk (the ALL_UNLOCKS loop re-grants freely).
    // if (!ARI.perks[_ctx.perk]) { ... }
}

// inside your latched register function (see Mod Anatomy):
mmapi_on("player.acquire_perk", perk_fanfare_player_acquire_perk);
```

## Engine Wiring

- Seam [`player_acquire_perk`](../seams/player_acquire_perk.md) dispatches from `gml/scripts/GameplaySystems/Player/Ari.gml`, at the head of `acquire_perk()`.

## See Also

- [player.essence_delta](player.essence_delta.md) - The Dragonshrine purchase's essence cost routes through this filter, right before the perk is acquired.
- [player.max_health_item](player.max_health_item.md) - Know when an item permanently raises Ari's max health.
- [player.skill_leveled](player.skill_leveled.md) - Know the moment the player levels up a skill.
