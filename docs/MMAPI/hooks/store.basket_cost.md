# Hook: store.basket_cost

Change the gold total a shop charges for the basket.

`store.basket_cost` is a **filter** hook. Register a callback with `mmapi_filter`. See [Hooks](../HOOKS.md) for how registration and dispatch work.

## Contract

Fires in `StoreMenu.update_prices()`, after the basket total is summed and before it is displayed, on every price pass. The filtered value is the basket total, the sum over basket slots of `item.store_value()` times the store's `price_markup` times the slot count. ctx is `{ menu }`. Return a replacement number, or `undefined` to keep the current value.

The result is the one number the receipt and the checkout read. It sets the receipt total and its can't-afford colour, the remaining-gold figure the shelf colouring compares against, the Buy gate, and the gold charged when Buy is pressed.

| | |
| --- | --- |
| **Fires** | In `update_prices()`, on menu init, category select, shelf tap, and basket pop or drain. |
| **Value** | The basket total in gold, markup already applied. |
| **ctx** | `{ menu }` |
| **Kind contract** | The callback receives the current value and returns a replacement, or `undefined` to keep the current value. |

### The ctx struct

- `menu` - the `StoreMenu`. `menu.store.id` is the `Store` id, `menu.basket` is the `Inventory` cart, and `menu.price_markup` is the store's markup.

### What the value does not cover

- Per-item shelf labels, the store tooltip price, and the purchase stat's cost read `store_value()` per item. [items.store_price](items.store_price.md) covers those.
- A return of exactly 0 locks the Buy button, because the gate requires a positive total.
- `set_gold` floors and truncates the charge, but the receipt shows the returned number as is. Return an integer.

## Usage

```gml
// store.basket_cost is a FILTER: you receive (value, ctx) and return a
// replacement, or undefined to keep the game's value.
function my_mod_store_basket_cost(_value, _ctx) {
    // _value is the basket total in gold, markup already applied.
    // _ctx is { menu }.
    //   .menu - the StoreMenu. Read .menu.store.id for the store,
    //           .menu.basket for the cart, .menu.price_markup for the markup.
    // e.g. ten percent off, never below one gold so Buy stays unlocked:
    // if (_value > 0) return max(1, floor(_value * 0.9));
    return undefined; // undefined = keep the game's value
}

// inside your latched register function (see Mod Anatomy):
mmapi_filter("store.basket_cost", my_mod_store_basket_cost);
```

## Interactions

- [items.store_price](items.store_price.md) runs inside the sum, once per basket slot, before this hook sees the total. A per-item price change lands in the value this hook receives.
- [player.gold_delta](player.gold_delta.md) receives the final total as one negative delta when Buy is pressed, after this hook's last pass.
- Change mod state from [store.item_added](store.item_added.md) when a tap should affect the total. That event fires before the price pass that follows the tap, so this filter reads the new state on the same tap.
- Settle whatever a changed total stood for from [store.purchase](store.purchase.md). It carries the same total in `ctx.total`.

## Engine Wiring

- Seam [`store_basket_cost`](../seams/store_basket_cost.md) dispatches from `gml/scripts/UI/Anchor/Menus/StoreMenu.gml`, inside `update_prices()`, between the basket sum and the receipt label update.

## See Also

- [items.store_price](items.store_price.md) - Change the base price a store charges for an item.
- [store.purchase](store.purchase.md) - Know the moment a Buy press commits.
- [store.item_added](store.item_added.md) - Know when an item lands in the shopping basket.
- [player.gold_delta](player.gold_delta.md) - Filter every gold change, the checkout charge included.
