# Hook: items.give

Rewrite any item the player is about to receive.

`items.give` is a **filter** hook. Register a callback with `mmapi_filter`. See [Hooks](../HOOKS.md) for how registration and dispatch work.

## Contract

Fires at the top of `give_item()`, after the item is coerced to a `LiveItem`. The filtered value is the struct `{ item, item_id, count, show_toast, show_new_popup, play_sound }`. ctx is the player. Return the replacement struct. The seam tolerates an `undefined` return and re-reads each field defensively (`item_id` is informational and is not read back).

| | |
| --- | --- |
| **Fires** | At the top of `give_item()`, after the item is coerced to a `LiveItem`. |
| **Value** | `{ item, item_id, count, show_toast, show_new_popup, play_sound }` |
| **ctx** | The player (the `Ari` struct). |
| **Kind contract** | The callback receives the current value and returns a replacement, or `undefined` to keep the current value. |

### The value struct

- `item` - the `LiveItem` about to be given, already coerced from whatever the caller passed. Replace it to change what arrives.
- `item_id` - the item's id (`item.item_id`). Informational only: the seam never reads it back, so changing it does nothing. Replace `item` instead.
- `count` - how many units to give.
- `show_toast` - whether the pickup toast shows.
- `show_new_popup` - whether the "new item" popup shows.
- `play_sound` - whether the pickup sound plays.

### The ctx parameter

- ctx - the player (`self` inside `give_item()`, the `Ari` struct).

## Usage

```gml
// items.give is a FILTER: you receive (value, ctx) and return a
// replacement, or undefined to keep the game's value.
function double_harvest_items_give(_value, _ctx) {
    // _value is { item, item_id, count, show_toast, show_new_popup, play_sound }.
    //   .item           - the LiveItem about to be given (already coerced).
    //   .item_id        - informational; the seam does not read it back.
    //   .count          - how many units to give.
    //   .show_toast     - whether the pickup toast shows.
    //   .show_new_popup - whether the "new item" popup shows.
    //   .play_sound     - whether the pickup sound plays.
    // _ctx is the player (the Ari struct).
    if (_value == undefined) return undefined; // test undefined BEFORE anything else
    // The seam re-reads every field defensively, so a mutated _value works:
    // _value.count *= 2;
    // return _value;
    return undefined; // undefined = keep the game's value
}

mmapi_filter("items.give", double_harvest_items_give);
```

## Engine Wiring

- Seam [`items_give`](../seams/items_give.md) dispatches from `gml/scripts/GameplaySystems/Player/Ari.gml`, at the head of `give_item()` right after the `LiveItem` coercion. It re-reads `item`, `count`, `show_toast`, `show_new_popup`, and `play_sound` from the returned struct defensively (`item_id` is never read back).

## See Also

- [items.dropped](items.dropped.md) - Know what is about to drop into the world.
- [items.consumed](items.consumed.md) - Know every item the player eats.
- [store.item_added](store.item_added.md) - Know when an item lands in the shopping basket.
