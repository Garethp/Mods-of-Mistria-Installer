# Hook: store.purchase

Know the moment a Buy press commits.

`store.purchase` is an **event** hook. Register a callback with `mmapi_on`. See [Hooks](../HOOKS.md) for how registration and dispatch work.

## Contract

Fires in the `StoreMenu` Buy callback after the basket is drained, the purchase stats recorded, and the purchases are grouped into stacks, immediately before the engine gives the stacks and charges the total. ctx is `{ menu, items, total }`. Observation only.

Nothing can abort between this fire and the charge. The basket is already empty and `modify_gold` cannot fail. The hook fires before the give loop, so an inventory slot a handler frees here is available when the stacks land. `give_item` drops whatever does not fit at the player's feet.

| | |
| --- | --- |
| **Fires** | In the Buy tap callback, after the stack grouping and before the give loop, once per Buy press. |
| **ctx** | `{ menu, items, total }` |
| **Kind contract** | The callback observes the moment. Its return value is ignored. |

### The ctx struct

- `menu` - the `StoreMenu`. `menu.store.id` names the shop. The menu closes after the charge.
- `items` - the engine's `List` of `List`. Each inner `List` is one stack about to be given: `first()` is the `LiveItem` and `count()` is the stack size.
- `total` - the gold about to be charged, after [store.basket_cost](store.basket_cost.md). It is a copy of `menu.basket_total`, so writing to it changes nothing.

> [!WARNING]
> The engine gives from `items` and charges `menu.basket_total` right after this event. Do not modify either here. A changed total charges a price the receipt never showed and the Buy lock never checked. Change what is charged from [store.basket_cost](store.basket_cost.md) and what is given from [items.give](items.give.md).

## Usage

```gml
// store.purchase is an EVENT: the return value is ignored.
// You cannot change or stop it here; the return value is ignored.
function my_mod_store_purchase(_ctx) {
    // _ctx is { menu, items, total }.
    //   .menu  - the StoreMenu. .menu.store.id names the shop.
    //   .items - List of List. Each inner List is one stack: first() is
    //            the LiveItem, count() the stack size.
    //   .total - the gold about to be charged.
    // e.g. remember the last shop and what it charged:
    // my_mod_state().last_store = _ctx.menu.store.id;
    // my_mod_state().last_total = _ctx.total;
}

// inside your latched register function (see Mod Anatomy):
mmapi_on("store.purchase", my_mod_store_purchase);
```

## Interactions

- [items.give](items.give.md) fires once per stack after this hook, from the engine's give loop.
- [player.gold_delta](player.gold_delta.md) fires once after this hook, with the negated total as the delta.
- The shelf is rebuilt from scratch on every open. To change what the next visit shows, record the purchase here and act on the record from [store.stock](store.stock.md).
- [ui.menu_closed](ui.menu_closed.md) follows once the menu frees.

## Engine Wiring

- Seam [`store_purchase`](../seams/store_purchase.md) dispatches from `gml/scripts/UI/Anchor/Menus/StoreMenu.gml`, inside the Buy tap callback, after the stack grouping and before the give loop.

## See Also

- [store.basket_cost](store.basket_cost.md) - Change the gold total a shop charges for the basket.
- [store.item_added](store.item_added.md) - Know when an item lands in the shopping basket.
- [items.give](items.give.md) - Rewrite any item the player is about to receive.
- [player.perk_purchased](player.perk_purchased.md) - The shrine counterpart. It fires after a perk purchase settles.
