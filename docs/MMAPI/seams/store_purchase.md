# Seam: store_purchase

Emits the moment a Buy press commits, after the stacks are grouped and before they are given and charged.

`store_purchase` is a **template seam** (`op = "emit"`). It feeds [store.purchase](../hooks/store.purchase.md). Mod authors never write seams. You register handlers for the hooks they dispatch. See [Seams](../SEAMS.md).

## Placement

| | |
| --- | --- |
| **File** | `gml/scripts/UI/Anchor/Menus/StoreMenu.gml` |
| **Locator** | pristine context after the stack grouping loop in the Buy tap callback, before the `item_list_of_lists.for_each` give loop and the `ARI.modify_gold(-self.basket_total)` charge |
| **Op** | `emit` |
| **Feeds** | [`store.purchase`](../hooks/store.purchase.md) |
| **ctx built** | `{ menu: self, items: item_list_of_lists, total: self.basket_total }` |
| **Marker** | `mmapi_store_run_purchase` |

## The Edit

The Buy tap callback drains the basket, records the purchase stats, writes the once-per-year bans, groups the drained items into `item_list_of_lists` (one inner `List` per stack), gives each stack through `ARI.give_item`, charges `self.basket_total`, and closes the menu. The generated emit lands between the grouping and the give loop: `mmapi_emit("store.purchase", { menu: self, items: item_list_of_lists, total: self.basket_total })` in the uniform try/catch shape. `items` is the very `List` the give loop reads next, and `total` is the field the charge reads next, so a handler sees exactly what the engine is about to do and can free inventory slots before the stacks land.

The anchoring is the point. The basket is already empty at this site, and nothing between the emit and the charge can fail, so the fire means the purchase happens. With zero handlers the emit early-outs on an empty registry, leaving pristine behavior.

## See Also

- [store.purchase](../hooks/store.purchase.md) - This is the hook this seam dispatches.
- [store_basket_cost](store_basket_cost.md) - The filter that set the total this emit reports.
- [store_item_added](store_item_added.md) - The emit for each item that entered the basket.
- [items_give](items_give.md) - The struct filter each stack passes through right after this emit.
