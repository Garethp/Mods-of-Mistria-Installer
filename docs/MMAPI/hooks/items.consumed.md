# Hook: items.consumed

Know every item the player eats.

`items.consumed` is an **event** hook. Register a callback with `mmapi_on`. See [Hooks](../HOOKS.md) for how registration and dispatch work.

## Contract

Fires after the player eats an item, right after the `items_eaten` stat is recorded. ctx is the consumed `LiveItem`.

| | |
| --- | --- |
| **Fires** | After the player eats an item, right after the `items_eaten` stat is recorded. |
| **ctx** | The consumed `LiveItem`. |
| **Kind contract** | The callback observes the moment. Its return value is ignored. |

### The ctx parameter

- ctx - the `LiveItem` the player just ate (the eating state's `self.live_item`). The emit lands immediately after the engine pushes this item's `{ item, day, hour, minute }` record onto `GAME_STATS.items_eaten`, so the stat for this bite is already recorded when your handler runs.

## Usage

```gml
// items.consumed is an EVENT: mmapi calls you after it happens.
// You cannot change or stop it here; the return value is ignored.
function snack_journal_items_consumed(_ctx) {
    // _ctx is the consumed LiveItem.
    // The items_eaten stat entry for this item is already recorded.
    // your code here
}

// inside your latched register function (see Mod Anatomy):
mmapi_on("items.consumed", snack_journal_items_consumed);
```

## Engine Wiring

- Seam [`items_consumed`](../seams/items_consumed.md) dispatches from `gml/scripts/Player/AriFsm.gml`, in the player's eating state, right after the `GAME_STATS.items_eaten` push.

## See Also

- [items.use_guard](items.use_guard.md) - Veto an item use before any of it happens.
- [player.max_health_item](player.max_health_item.md) - This hook fires when an eaten item raises max health.
- [items.give](items.give.md) - Rewrite any item the player is about to receive.
