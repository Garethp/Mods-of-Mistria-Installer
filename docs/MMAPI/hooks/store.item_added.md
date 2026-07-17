# Hook: store.item_added

Know when an item lands in the shopping basket.

`store.item_added` is an **event** hook. Register a callback with `mmapi_on`. See [Hooks](../HOOKS.md) for how registration and dispatch work.

## Contract

Fires in `StoreMenu` right after a shelf tap adds an item to the shopping basket (`self.basket.add(item)`), before the ghost and price update. ctx is `{ menu, item }`: `menu` is the `StoreMenu` (`menu.basket` is the `Inventory` cart with `.can_add(item)`/`.add(item)`, `menu.update_prices()` refreshes the totals), `item` is the `LiveItem` just added.

Adding more from a handler via `menu.basket.add` does not re-fire this event (the emit is at the tap site, not at `basket.add`), so no re-entry guard is needed: a bulk-buy handler can add extra copies freely.

| | |
| --- | --- |
| **Fires** | In `StoreMenu`, right after a shelf tap adds an item to the basket, before the ghost and price update. |
| **ctx** | `{ menu, item }` |
| **Kind contract** | The callback observes the moment. Its return value is ignored. |

### The ctx struct

- `menu` - the `StoreMenu`. `menu.basket` is the `Inventory` cart (`.can_add(item)` / `.add(item)`), and `menu.update_prices()` refreshes the totals.
- `item` - the `LiveItem` the tap just added to the basket.

## Usage

```gml
// store.item_added is an EVENT: mmapi calls you after it happens.
// You cannot change or stop it here; the return value is ignored.
function bulk_buyer_store_item_added(_ctx) {
    // _ctx is { menu, item }.
    //   .menu - the StoreMenu; .menu.basket is the Inventory cart
    //           (.can_add(item) / .add(item)), and .menu.update_prices()
    //           refreshes the totals.
    //   .item - the LiveItem just added to the basket.
    // The emit is at the tap site, not at basket.add, so adding more here
    // does not re-fire the event - no re-entry guard needed:
    // if (keyboard_check(vk_shift)) {
    //     repeat (9) {
    //         if (_ctx.menu.basket.can_add(_ctx.item)) _ctx.menu.basket.add(_ctx.item);
    //     }
    //     _ctx.menu.update_prices();
    // }
}

// inside your latched register function (see Mod Anatomy):
mmapi_on("store.item_added", bulk_buyer_store_item_added);
```

## Engine Wiring

- Seam [`store_item_added`](../seams/store_item_added.md) dispatches from `gml/scripts/UI/Anchor/Menus/StoreMenu.gml`, immediately after `self.basket.add(item)` in the shelf tap callback, before the ghost and price update.

## See Also

- [items.give](items.give.md) - Rewrite any item the player is about to receive.
- [ui.menu_opened](ui.menu_opened.md) - Know when a menu (the store included) opens.
