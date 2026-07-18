# Hook: items.trashed

Know the moment the player trashes an item.

`items.trashed` is an **event** hook. Register a callback with `mmapi_on`. See [Hooks](../HOOKS.md) for how registration and dispatch work.

## Contract

Fires when the player trashes the item in hand slot 0 via a trash button: the inventory trash button (`PlayerMenu`, the journal inventory) or the storage trash button (`StorageMenu`, chests). It fires at the top of the button's tap callback, before the slot `pop()`/`drain()` removes the item(s).

ctx is `{ item, count }`: `item` is the `LiveItem` in hand slot 0 (the item being trashed, or `undefined` if the slot is somehow empty), and `count` is how many units this tap trashes (the whole stack when Shift-trashing via `drain`, else 1).

This is the real trash signal: the generic slot `pop()`/`drain()` are shared by every inventory move, so only these two trash-button callbacks distinguish a trash from an ordinary slot operation (the game itself marks them with the `SoundEffects/UI/UITrashItem` tap sound). This is an event, so the trash still happens. Use it to react (e.g. pay out the trashed item's value).

| | |
| --- | --- |
| **Fires** | At the top of the two trash buttons' tap callbacks (`PlayerMenu` inventory, `StorageMenu` chests), before the slot `pop()`/`drain()`. |
| **ctx** | `{ item, count }` |
| **Kind contract** | The callback observes the moment. Its return value is ignored. |

### The ctx struct

- `item` - the `LiveItem` in hand slot 0, captured while the slot still holds it (the item being trashed). `undefined` if the slot is somehow empty.
- `count` - how many units this tap trashes: the whole stack (`slot(0).count`) when Shift is held (the `drain` path), else 1 (the `pop` path).

## Usage

```gml
// items.trashed is an EVENT: the return value is ignored.
// You cannot change or stop it here; the return value is ignored.
function trash_for_cash_items_trashed(_ctx) {
    // _ctx is { item, count }.
    //   .item  - the LiveItem in hand slot 0, the item being trashed
    //            (undefined if the slot is somehow empty).
    //   .count - units this tap trashes: the whole stack when
    //            Shift-trashing (drain), else 1 (pop).
    // The trash still happens - react to it, e.g. pay out the item's value:
    // if (_ctx.item != undefined) {
    //     // credit _ctx.count * <value of _ctx.item>
    // }
}

// inside your latched register function (see Mod Anatomy):
mmapi_on("items.trashed", trash_for_cash_items_trashed);
```

## Engine Wiring

- Seam [`inventory_trash_button`](../seams/inventory_trash_button.md) dispatches from `gml/scripts/UI/Anchor/Menus/PlayerMenu.gml`, at the top of the inventory trash button's tap callback, before the `self.inventory.hand.slot(0)` `pop()`/`drain()`.
- Seam [`storage_trash_button`](../seams/storage_trash_button.md) dispatches from `gml/scripts/UI/Anchor/Menus/StorageMenu.gml`, the same shape for the chest trash button (`hand` there is `self.hand`).

## See Also

- [items.dropped](items.dropped.md) - Know what is about to drop into the world.
- [items.give](items.give.md) - Rewrite any item the player is about to receive.
