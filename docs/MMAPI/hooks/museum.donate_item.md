# Hook: museum.donate_item

Know when an item is donated to the museum.

`museum.donate_item` is an **event** hook. Register a callback with `mmapi_on`. See [Hooks](../HOOKS.md) for how registration and dispatch work.

## Contract

Fires at the top of `donate_item_to_museum()`, before the item is registered to the collection and before the pending renown entry is pushed. ctx is `{ item_id }`.

This hook is observation only. The `DonationResult` the function goes on to compute (progress made, completed set, rewards) is decided after the emit, so a handler sees the donation before the museum knows what it amounts to. It fires once per donated item, from the museum donation menu (and the engine's test suite). Save load and the `ALL_UNLOCKS` new-game path write donations through `register_item_to_museum()` directly and never fire this hook, so handlers see genuine player donations only.

| | |
| --- | --- |
| **Fires** | At the top of `donate_item_to_museum()`, before the item is registered or the renown entry is pushed. |
| **ctx** | `{ item_id }` |
| **Kind contract** | The callback observes the moment. Its return value is ignored. |

### The ctx struct

- `item_id` - the `ItemId` being donated.

## Usage

```gml
// museum.donate_item is an EVENT: the return value is ignored.
// You cannot change or stop it here; the return value is ignored.
function curator_ledger_museum_donate_item(_ctx) {
    // _ctx is { item_id }.
    //   .item_id - the ItemId being donated.
    // The donation has not been written yet: MUSEUM_PROGRESS[_ctx.item_id]
    // still reads false here, and flips right after the emit.
    // if (_ctx.item_id == <your tracked item>) { ... }
}

// inside your latched register function (see Mod Anatomy):
mmapi_on("museum.donate_item", curator_ledger_museum_donate_item);
```

## Engine Wiring

- Seam [`museum_donate_item`](../seams/museum_donate_item.md) dispatches from `gml/scripts/Museum.gml`, at the head of `donate_item_to_museum()`.

## See Also

- [player.renown_delta](player.renown_delta.md) - This filter is where the donation's renown lands at day rollover, one pending entry per donation.
- [quest.complete](quest.complete.md) - This event is the other progression moment that queues a pending renown entry.
- [store.item_added](store.item_added.md) - Know when an item lands in the shopping basket.
