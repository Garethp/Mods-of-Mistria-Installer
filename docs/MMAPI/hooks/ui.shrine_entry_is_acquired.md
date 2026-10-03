# Hook: ui.shrine_entry_is_acquired

Change whether a shrine entry counts as already bought.

`ui.shrine_entry_is_acquired` is a **filter** hook. Register a callback with `mmapi_filter`. See [Hooks](../HOOKS.md) for how registration and dispatch work.

## Contract

Fires at the return of `DragonShrineMenu.entry_is_acquired(entry)`. The filtered value is the boolean the shrine menus use to treat an entry as already bought, which pristine reads straight from `ARI.perks[entry.perk]`. ctx is `{ perk }`, the entry's `Perk` enum id. Return the replacement boolean, or `undefined` to keep the current value.

The menu asks this for every tile on the tier screen every frame, and the answer picks the tile's tint, decides whether the entry popup offers the Learn button or the Enable and Disable toggle, and shows or hides the cost. Forcing `false` on an owned perk lets the player buy it again, which spends the essence and runs `Ari.acquire_perk()` as a re-grant. Forcing `true` hides the Learn button without writing `ARI.perks`. The perk's effect is never read through this predicate.

| | |
| --- | --- |
| **Fires** | At the return of `DragonShrineMenu.entry_is_acquired(entry)`. |
| **Value** | The boolean the shrine menus use to treat the entry as already bought. |
| **ctx** | `{ perk }` |
| **Kind contract** | The callback receives the current value and returns a replacement, or `undefined` to keep the current value. |

### The ctx struct

- `perk` - the `Perk` enum id of the entry being asked about.

## Usage

```gml
// ui.shrine_entry_is_acquired is a FILTER: you receive (value, ctx) and return a
// replacement, or undefined to keep the game's value.
function second_helping_ui_shrine_entry_is_acquired(_value, _ctx) {
    // _value is the boolean entry_is_acquired() just computed (ARI.perks[perk]).
    // _ctx is { perk }.
    if (_value == undefined) return undefined; // test undefined BEFORE anything else
    // This runs per tile per frame while the tier screen is open, so look
    // your perk up first and return undefined fast when it is not yours.
    // Offer an owned perk for sale again:
    // if (_ctx.perk == Perk.GuardiansShield && <your condition>) return false;
    return undefined; // undefined = keep the game's value
}

mmapi_filter("ui.shrine_entry_is_acquired", second_helping_ui_shrine_entry_is_acquired);
```

## Interactions

- Forcing `false` on an owned perk makes the popup offer Learn. The purchase spends the essence through `player.essence_delta`, runs `Ari.acquire_perk()` as a re-grant, and fires `player.perk_acquired` and `player.perk_purchased` like a first purchase.
- Forcing `true` on an unowned perk makes the popup show the Enable and Disable toggle instead. The toggle reads `ARI.perk_active()`, which is false for an unowned perk, so it labels itself Enable and its tap writes `ARI.perks_active[perk] = true` with no effect on play.
- The value only steers the shrine menus. Every gameplay check of a perk goes through `ARI.perk_active()` and never asks this predicate.
- The predicate runs for every tile on the tier screen every frame, the same hot path as `ui.hud_should_show`, so handlers must be cheap.

## Engine Wiring

- Seam [`ui_shrine_entry_is_acquired`](../seams/ui_shrine_entry_is_acquired.md) dispatches from `gml/scripts/UI/Anchor/Menus/DragonshrineMenu.gml`, a whole-function wrap of `entry_is_acquired()` that filters its return value.

## See Also

- [player.perk_purchased](player.perk_purchased.md) - Know when the player buys a perk at a shrine.
- [player.perk_acquired](player.perk_acquired.md) - Know when the player has acquired a perk.
- [ui.hud_should_show](ui.hud_should_show.md) - Change whether the HUD shows, the same filtered predicate shape.
