# Hook: items.store_price

Change the base price a store charges for an item.

`items.store_price` is a **filter** hook. Register a callback with `mmapi_filter`. See [Hooks](../HOOKS.md) for how registration and dispatch work.

## Contract

Fires at the return of `LiveItem.store_value()`, the base buy price a store charges for an item before the store's markup. The filtered value is the number `store_value()` computed, which is the prototype's `value.store`, the star price for a recipe scroll, or a cosmetic's price override, with the `DiscountTreats` perk already applied. ctx is `{ item, menu }`.

Return a replacement number, or `undefined` to keep the current value.  The store's markup multiplies on top of the returned value.

| | |
| --- | --- |
| **Fires** | At the return of `LiveItem.store_value()`, for every shelf item and basket slot on each price pass. |
| **Value** | The base buy price in gold, before the store's markup. |
| **ctx** | `{ item, menu }` |
| **Kind contract** | The callback receives the current value and returns a replacement, or `undefined` to keep the current value. |

### The ctx struct

- `item` - the `LiveItem` being priced (`self` inside `store_value()`).
- `menu` - the `StoreMenu` pricing the item. `menu.store.id` is the `Store` id and `menu.price_markup` is the store's markup. It is set on every fire, the constructor pass included.

> [!WARNING]
> `ctx.item` is the live `LiveItem`, and its `prototype` is the one struct every item with that id shares. The hook filters the returned number and never writes to the prototype. A handler that modifies `_ctx.item.prototype.value.store` changes that item's price for every store until the game restarts.

### Where the value lands

- The shelf price label under each icon, as `round(value * price_markup)`.
- The basket sum, which [store.basket_cost](store.basket_cost.md) then filters.
- The store tooltip price.
- The cost recorded in the purchase stats when Buy is pressed.

> [!NOTE]
> The sell price, `bin_value()`, never passes through this hook.

## Usage

```gml
// items.store_price is a FILTER: you receive (value, ctx) and return a
// replacement, or undefined to keep the game's value.
function my_mod_items_store_price(_value, _ctx) {
    // _value is the base buy price, before the store's markup.
    // _ctx is { item, menu }.
    //   .item - the LiveItem being priced. Read .item.item_id or
    //           .item.prototype.
    //   .menu - the StoreMenu pricing it. Read .menu.store.id for the shop.
    // Fires per shelf item and basket slot on every price pass, so test
    // cheap conditionions first:
    // if (_ctx.menu.store.id != Store.General) return undefined;
    // e.g. every food item costs 5 more at the general store:
    // if (_ctx.item.prototype.tags.contains("food")) return _value + 5;
    return undefined; // undefined = keep the game's value
}

// inside your latched register function (see Mod Anatomy):
mmapi_filter("items.store_price", my_mod_items_store_price);
```

## Interactions

- Read the store from `ctx.menu` rather than from `ANCHOR.get_menu(Menu.Store)`. The first price pass runs inside the `StoreMenu` constructor, before ANCHOR pushes the menu onto `open_menus`, so `get_menu` is `undefined` for the initial shelf labels, the initial basket sum, and the first tooltip.
- The engine runs the price pass only on player actions, which are opening the shop, switching tabs, and adding to or removing from the basket. Each pass rewrites the shelf label text, the open tooltip's price, the label colour, the receipt, and the Buy lock.
- The Buy press charges the last stored total without recomputing. Call `menu.update_prices()` whenever your handler's answer changes while the shop is open. The pending basket is then displayed and charged at the new price instead of waiting for the next basket change.
- The tooltip's title, description, stars, and icon are built once at spawn and are not refreshed. Only its price is.
- [store.basket_cost](store.basket_cost.md) filters the sum of these values times the markup and the slot count, after this hook.
- Return the base price and leave the markup to the engine. Every store markup multiplies after this hook, so the markup amount never passes through a handler. Read the markup from `ctx.menu.price_markup` when a replacement depends on it.
- The `DiscountTreats` perk is applied inside the value, so a replacement replaces the discounted price.
- [ui.item_icon](ui.item_icon.md) and [item.display_description](item.display_description.md) are the sibling wraps on the same struct. The three fire independently.

## Engine Wiring

- Seam [`items_store_price`](../seams/items_store_price.md) dispatches from `gml/scripts/GameplaySystems/Items/LiveItem.gml`, a whole-function wrap of `store_value()` that filters its return value.
- Companion seam [`store_menu_ctx_pointer`](../seams/store_menu_ctx_pointer.md) provides no dispatch of its own. It holds the `StoreMenu` in a framework global for the span of the constructor's `init()` call, so `ctx.menu` is filled during the pass `ANCHOR.get_menu` cannot see.
- Companion seam [`store_shelf_label_refresh`](../seams/store_shelf_label_refresh.md) provides no dispatch of its own. It has the price pass rewrite each shelf label's price text, so a filtered price that changes while the shop is open reaches the shelf.
- Companion seam [`store_tooltip_price_refresh`](../seams/store_tooltip_price_refresh.md) provides no dispatch of its own. It has the same price pass rewrite the open tooltip's price node, so the change reaches the tooltip without a re-spawn.

## See Also

- [store.basket_cost](store.basket_cost.md) - Change the gold total a shop charges for the basket.
- [store.purchase](store.purchase.md) - Know the moment a Buy press commits.
- [item.display_description](item.display_description.md) - Reword the description an item's tooltip renders.
- [ui.item_icon](ui.item_icon.md) - Swap an item's icon sprite wherever it is resolved.
