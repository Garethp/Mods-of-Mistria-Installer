# Hook: items.use_guard

Block an item from being used.

`items.use_guard` is a **guard** hook. Register a callback with `mmapi_guard`. See [Hooks](../HOOKS.md) for how registration and dispatch work.

## Contract

Fires at the top of `use_item()`, after the item is coerced to a `LiveItem` and before any use logic. ctx is the `LiveItem` being used. Return `false` to veto the use (`use_item` returns `false`). `undefined` or `true` allows.

| | |
| --- | --- |
| **Fires** | At the top of `use_item()`, after the `LiveItem` coercion and before any use logic. |
| **ctx** | The `LiveItem` being used. |
| **Kind contract** | The callback returns `false` to veto the action. `undefined` or `true` allows it. Guards fail open: a callback that throws counts as allow. |

### The ctx parameter

- ctx - the `LiveItem` being used. Already coerced: the guard sits immediately after `if !is_struct(item) { item = new LiveItem(item); }`, so raw item ids the caller passed have been wrapped and ctx is always a `LiveItem`.

## Usage

```gml
// items.use_guard is a GUARD: return false to block it, undefined (or true)
// to allow. Guards fail OPEN - if your handler crashes, the action happens.
function sealed_relics_items_use_guard(_ctx) {
    // _ctx is the LiveItem being used (always a LiveItem - raw ids are
    // coerced before the guard runs).
    // if (<your condition>) {
    //     return false; // veto - the engine then runs: return false;
    // }
    return undefined; // allow everything else
}

// inside your latched register function (see Mod Anatomy):
mmapi_guard("items.use_guard", sealed_relics_items_use_guard);
```

## Engine Wiring

- Seam [`items_use_guard`](../seams/items_use_guard.md) dispatches from `gml/scripts/GameplaySystems/Items/use_item.gml`, anchored immediately after the `LiveItem` coercion at the head of `use_item()`. On veto the engine runs `return false;`.

## See Also

- [items.consumed](items.consumed.md) - Know every item the player eats.
- [furniture.place_guard](furniture.place_guard.md) - Veto a furniture placement before it is written.
- [items.give](items.give.md) - Rewrite any item the player is about to receive.
